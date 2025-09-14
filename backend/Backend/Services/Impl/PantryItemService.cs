using System.ComponentModel.DataAnnotations;
using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class PantryItemService(PlannerContext plannerContext, ILogger<PantryItemService> logger) : IPantryItemService
    {
        private readonly PlannerContext _context = plannerContext;
        private readonly ILogger<PantryItemService> _logger = logger;

        public async Task<PantryItemDto> CreatePantryItemAsync(CreateUpdatePantryItemRequestDto pantryItemDto, int userId)
        {
            _logger.LogInformation("Creating for user {UserId} pantry item {dtoItem}", userId, pantryItemDto);
            PantryItem entity = await CreatePantryItem(pantryItemDto, userId);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pantry item created");

            return entity.ToDto();
        }

        public async Task<GetPantryItemsResult> CreatePantryItemsAsync(IEnumerable<CreateUpdatePantryItemRequestDto> pantryItemDtos, int userId)
        {
            _logger.LogInformation("Creating for user {id} {count} pantry items", userId, pantryItemDtos.Count());

            if (pantryItemDtos.Any(dto => dto.Food == null))
                _logger.LogWarning("Pantry items with no foods were added. These will be filtered out.");

            var tasks = pantryItemDtos.Where(p => p != null && p.Food != null).Select(async dto => await CreatePantryItem(dto, userId));
            var pantryItems = await Task.WhenAll(tasks);

            int count = await _context.SaveChangesAsync();

            _logger.LogInformation("{count} pantry items created.", count);

            return new GetPantryItemsResult { TotalCount = pantryItems.Length, Items = pantryItems.Select(i => i.ToDto()) };
        }

        public async Task<bool> DeletePantryItemAsync(int id)
        {
            _logger.LogInformation("Deleting pantry item {id}", id);
            var entity = await _context.PantryItems.FindAsync(id);
            if (entity is null) return false;

            _context.PantryItems.Remove(entity);
            var deleted = await _context.SaveChangesAsync();
            _logger.LogInformation("{deleted} pantry items deleted", deleted);
            return deleted > 0;
        }

        public async Task<DeleteRequest> DeletePantryItemsAsync(IEnumerable<int> ids)
        {
            _logger.LogInformation("Deleting {count} pantry items", ids.Count());
            var entities = _context.PantryItems.Where(p => ids.Contains(p.Id));
            var deletedIds = entities.Select(e => e.Id).ToList();
            _context.PantryItems.RemoveRange(entities);
            int count = await _context.SaveChangesAsync();
            _logger.LogInformation("{count} pantry items deleted", count);
            return new DeleteRequest { Ids = deletedIds };
        }

        public async Task<GetPantryItemsResult> GetAllPantryItemsAsync(int? skip, int? take)
        {
            _logger.LogInformation("Getting {take} pantry items, skip {skip}", take, skip);
            var query = _context.PantryItems.AsNoTracking().Include(i => i.Food).OrderBy(p => p.Food.Name).AsQueryable();

            var totalCount = await query.CountAsync();

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogWarning("Negative skip used for get all.");
                    throw new ArgumentException("Non-negative skip must be used for pagination.");
                }

                query = query.Skip(skip.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogWarning("Negative take used for get all.");
                    throw new ArgumentException("Non-negative take must be used for pagination.");
                }

                query = query.Take(take.Value);
            }

            var items = await query.ToListAsync();

            return new GetPantryItemsResult { Items = items.Select(i => i.ToDto()), TotalCount = totalCount };
        }

        public async Task<PantryItemDto?> GetPantryItemByIdAsync(int id)
        {
            _logger.LogInformation("Getting pantry item {id}", id);
            var entity = await _context.PantryItems
                .AsNoTracking()
                .Include(i => i.Food)
                .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            return entity?.ToDto();
        }

        public async Task<PantryItemDto> UpdatePantryItemAsync(CreateUpdatePantryItemRequestDto pantryItemDto, int userId)
        {
            if (pantryItemDto == null)
            {
                _logger.LogWarning("pantryItem is required");
                throw new ArgumentException("pantryItem is required.");
            }

            if (pantryItemDto.Id == null)
            {
                _logger.LogWarning("PantryItemDto.Id is required for updates.");
                throw new ArgumentException("PantryItemDto.Id is required for updates.");
            }

            var item = _context.PantryItems.FirstOrDefault(item => item.Id == pantryItemDto.Id && item.UserId == userId);
            if (item == null)
            {
                _logger.LogWarning("Could not find pantry item {id} to update.", pantryItemDto.Id);
                throw new ArgumentException("Could not find pantry item {0} to update.", pantryItemDto.Id.ToString());
            }

            if (pantryItemDto.Quantity < 0)
            {
                _logger.LogWarning("Cannot set pantry item with negative quantity.");
                throw new ArgumentException("Cannot set pantry item with negative quantity.");
            }

            item.Quantity = pantryItemDto.Quantity;
            item.Unit = pantryItemDto.Unit;
            item.FoodId = await UpdatePantryItemFood(pantryItemDto);

            await _context.SaveChangesAsync();
            return item.ToDto();
        }

        public async Task<GetPantryItemsResult> Search(string search, int userId, int? take, int? skip)
        {
            var items = _context.PantryItems
                .Where(i => i.UserId == userId)
                .Where(i => i.Food.Name.Contains(search.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(i => i.Food.Name)
                .AsQueryable();

            var count = await items.CountAsync();

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogWarning("Negative skip used for search.");
                    throw new ArgumentException("Non-negative skip must be used for pagination.");
                }

                items = items.Skip(skip.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogWarning("Negative take used for search.");
                    throw new ArgumentException("Non-negative take must be used for pagination.");
                }

                items = items.Take(take.Value);
            }

            var results = await items.ToListAsync();
            return new GetPantryItemsResult { TotalCount = count, Items = items.Select(i => i.ToDto()) };
        }

        private async Task<int> UpdatePantryItemFood(CreateUpdatePantryItemRequestDto pantryItemDto)
        {
            int foodId;

            if (pantryItemDto.Food.Mode == AddFoodMode.Existing)
            {
                var food = (ExistingFoodReferenceDto)pantryItemDto.Food;
                foodId = food.Id;
                if (await _context.Foods.FirstOrDefaultAsync(i => i.Id == foodId) == null)
                {
                    _logger.LogWarning("FoodId provided was not valid.");
                    throw new ValidationException("FoodId provided was not valid.");
                }
            }
            else if (pantryItemDto.Food.Mode == AddFoodMode.New)
            {
                var food1 = (NewFoodReferenceDto)pantryItemDto.Food;
                if (await _context.Categories.FirstOrDefaultAsync(c => c.Id == food1.CategoryId) == null)
                {
                    _logger.LogWarning("FoodName was provided without CategoryId");
                    throw new ArgumentException("CategoryId is required on pantry items with new foods.");
                }

                foodId = await CreateNewFood(food1);
            }
            else
            {
                _logger.LogError("FoodId or FoodName must be provided.");
                throw new ArgumentException("FoodId or FoodName must be provided.");
            }

            return foodId;
        }

        private async Task<PantryItem> CreatePantryItem(CreateUpdatePantryItemRequestDto pantryItemDto, int userId)
        {
            if (pantryItemDto.Quantity < 0)
            {
                _logger.LogWarning("Cannot set pantry item with negative quantity.");
                throw new ArgumentException("Cannot set pantry item with negative quantity.");
            }

            int foodId = await UpdatePantryItemFood(pantryItemDto);

            var entity = new PantryItem
            {
                FoodId = foodId,
                Quantity = pantryItemDto.Quantity,
                Unit = pantryItemDto.Unit,
                UserId = userId
            };

            _context.PantryItems.Add(entity);
            return entity;
        }

        private async Task<int> CreateNewFood(NewFoodReferenceDto dto)
        {
            var food = new Food
            {
                Name = dto.Name!,
                CategoryId = dto.CategoryId
            };

            _context.Foods.Add(food);
            await _context.SaveChangesAsync();
            return food.Id;
        }
    }
}