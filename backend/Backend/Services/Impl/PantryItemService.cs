using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class PantryItemService(PlannerContext plannerContext, ILogger<PantryItemService> logger) : IPantryItemService
    {
        private readonly PlannerContext _context = plannerContext;
        private readonly ILogger<PantryItemService> _logger = logger;

        public async Task<PantryItemDto> CreatePantryItemAsync(CreateUpdatePantryItemRequestDto pantryItemDto, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering CreatePantryItemAsync: userId={UserId}, dto={Dto}", userId, pantryItemDto);
            if (_context.Users.Find(userId) == null)
            {
                _logger.LogWarning("CreatePantryItemAsync: Invalid userId {UserId}", userId);
                throw new ArgumentException("UserId provided was not valid.");
            }

            if (pantryItemDto == null)
            {
                _logger.LogWarning("CreatePantryItemAsync: pantryItemDto is null for userId {UserId}", userId);
                throw new ArgumentException("pantryItem is required.");
            }

            if (pantryItemDto.Id != null)
            {
                _logger.LogWarning("CreatePantryItemAsync: PantryItemDto.Id must be null for creates. userId={UserId}, id={Id}", userId, pantryItemDto.Id);
                throw new ArgumentException("PantryItemDto.Id must be null for creates.");
            }

            if (pantryItemDto.Quantity < 0)
            {
                _logger.LogWarning("CreatePantryItemAsync: Negative quantity {Quantity} for userId {UserId}", pantryItemDto.Quantity, userId);
                throw new ArgumentException("Cannot set pantry item with negative quantity.");
            }

            if (pantryItemDto.Food == null)
            {
                _logger.LogWarning("CreatePantryItemAsync: Food is null for userId {UserId}", userId);
                throw new ArgumentException("Food is required.");
            }

            // transaction is probably overkill, but since we re-query the context to ensure we have the Food, I want it to be able to
            // roll back if there's an issue.
            var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                _logger.LogInformation("CreatePantryItemAsync: Creating pantry item for userId={UserId}, dto={Dto}", userId, pantryItemDto);
                PantryItem entity = await CreatePantryItem(pantryItemDto, userId);
                await _context.SaveChangesAsync(ct);

                var result = await _context.PantryItems
                    .AsNoTracking()
                    .Include(p => p.Food)
                    .ThenInclude(f => f.Category)
                    .FirstOrDefaultAsync(p => p.Id == entity.Id, ct);

                _logger.LogInformation("CreatePantryItemAsync: Pantry item created for userId={UserId}, itemId={ItemId}", userId, entity.Id);
                _logger.LogInformation("Exiting CreatePantryItemAsync: userId={UserId}, itemId={ItemId}", userId, entity.Id);
                await transaction.CommitAsync(ct);
                return result?.ToDto()!;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<GetPantryItemsResult> CreatePantryItemsAsync(IEnumerable<CreateUpdatePantryItemRequestDto> pantryItemDtos, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering CreatePantryItemsAsync: userId={UserId}, count={Count}", userId, pantryItemDtos.Count());

            if (pantryItemDtos.Any(dto => dto.Food == null))
                _logger.LogWarning("CreatePantryItemsAsync: Some pantry items have null Food for userId={UserId}", userId);

            var dtoList = pantryItemDtos.Where(p => p != null && p.Food != null).ToList();
            var pantryItems = new List<PantryItem>();
            foreach (var d in dtoList)
            {
                var pantryItem = await CreatePantryItem(d, userId);
                pantryItems.Add(pantryItem);
            }

            int count = await _context.SaveChangesAsync(ct);

            _logger.LogInformation("CreatePantryItemsAsync: {Count} pantry items created for userId={UserId}", count, userId);
            _logger.LogInformation("Exiting CreatePantryItemsAsync: userId={UserId}, totalCreated={Total}", userId, pantryItems.Count);
            return new GetPantryItemsResult { TotalCount = pantryItems.Count, Items = pantryItems.Select(i => i.ToDto()) };
        }

        public async Task<bool> DeletePantryItemAsync(int id, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering DeletePantryItemAsync: itemId={ItemId}", id);
            var entity = await _context.PantryItems.FindAsync([id], ct);
            if (entity is null) return false;

            _context.PantryItems.Remove(entity);
            var deleted = await _context.SaveChangesAsync(ct);
            _logger.LogInformation("DeletePantryItemAsync: {Deleted} pantry items deleted, itemId={ItemId}", deleted, id);
            _logger.LogInformation("Exiting DeletePantryItemAsync: itemId={ItemId}, deleted={Deleted}", id, deleted);
            return deleted > 0;
        }

        public async Task<DeleteRequest> DeletePantryItemsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering DeletePantryItemsAsync: count={Count}", ids.Count());
            var entities = _context.PantryItems.Where(p => ids.Contains(p.Id));
            var deletedIds = await entities
                .Select(e => e.Id)
                .ToListAsync(ct);

            _context.PantryItems.RemoveRange(entities);
            int count = await _context.SaveChangesAsync(ct);

            _logger.LogInformation("DeletePantryItemsAsync: {Count} pantry items deleted", count);
            _logger.LogInformation("Exiting DeletePantryItemsAsync: deletedIds={DeletedIds}", string.Join(",", deletedIds));
            return new DeleteRequest { Ids = deletedIds };
        }

        public async Task<GetPantryItemsResult> GetAllPantryItemsAsync(int? skip, int? take, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering GetAllPantryItemsAsync: take={Take}, skip={Skip}", take, skip);
            var query = _context.PantryItems
                .AsNoTracking()
                .Include(i => i.Food)
                .OrderBy(p => p.Food.Name)
                .AsQueryable();

            var totalCount = await query.CountAsync(ct);

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogWarning("GetAllPantryItemsAsync: Negative skip {Skip}", skip);
                    throw new ArgumentException("Non-negative skip must be used for pagination.");
                }

                query = query.Skip(skip.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogWarning("GetAllPantryItemsAsync: Negative take {Take}", take);
                    throw new ArgumentException("Non-negative take must be used for pagination.");
                }

                query = query.Take(take.Value);
            }

            var items = await query.ToListAsync(ct);

            _logger.LogInformation("Exiting GetAllPantryItemsAsync: returned {Count} items", items.Count);
            return new GetPantryItemsResult { Items = items.Select(i => i.ToDto()), TotalCount = totalCount };
        }

        public async Task<PantryItemDto?> GetPantryItemByIdAsync(int id, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering GetPantryItemByIdAsync: itemId={ItemId}", id);
            var entity = await _context.PantryItems
                .AsNoTracking()
                .Include(i => i.Food)
                .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            _logger.LogInformation("Exiting GetPantryItemByIdAsync: itemId={ItemId}, found={Found}", id, entity != null);
            return entity?.ToDto();
        }

        public async Task<PantryItemDto> UpdatePantryItemAsync(CreateUpdatePantryItemRequestDto pantryItemDto, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering UpdatePantryItemAsync: userId={UserId}, dto={Dto}", userId, pantryItemDto);
            if (pantryItemDto == null)
            {
                _logger.LogWarning("UpdatePantryItemAsync: pantryItemDto is null for userId {UserId}", userId);
                throw new ArgumentException("pantryItem is required.");
            }

            if (pantryItemDto.Id == null)
            {
                _logger.LogWarning("UpdatePantryItemAsync: PantryItemDto.Id is required for updates. userId={UserId}", userId);
                throw new ArgumentException("PantryItemDto.Id is required for updates.");
            }

            var item = await _context.PantryItems.FirstOrDefaultAsync(item => item.Id == pantryItemDto.Id && item.UserId == userId, ct);
            if (item == null)
            {
                _logger.LogWarning("UpdatePantryItemAsync: Could not find pantry item {Id} for userId {UserId}", pantryItemDto.Id, userId);
                throw new ArgumentException($"Could not find pantry item {pantryItemDto.Id} to update.");
            }

            if (pantryItemDto.Quantity < 0)
            {
                _logger.LogWarning("UpdatePantryItemAsync: Negative quantity {Quantity} for userId {UserId}", pantryItemDto.Quantity, userId);
                throw new ArgumentException("Cannot set pantry item with negative quantity.");
            }

            item.Quantity = pantryItemDto.Quantity;
            item.Unit = pantryItemDto.Unit;
            item.FoodId = await UpdatePantryItemFood(pantryItemDto);

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("UpdatePantryItemAsync: Updated pantry item for userId={UserId}, itemId={ItemId}", userId, item.Id);
            _logger.LogInformation("Exiting UpdatePantryItemAsync: userId={UserId}, itemId={ItemId}", userId, item.Id);
            return item.ToDto();
        }

        public async Task<GetPantryItemsResult> Search(string search, int userId, int? take, int? skip, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering Search: userId={UserId}, search={Search}, take={Take}, skip={Skip}", userId, search, take, skip);
            var items = _context.PantryItems
                .Where(i => i.UserId == userId)
                .Where(i => i.Food.Name.ToLower().Contains(search.ToLower()))
                .OrderBy(i => i.Food.Name)
                .AsQueryable();

            var count = await items.CountAsync(ct);

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogWarning("Search: Negative skip {Skip}", skip);
                    throw new ArgumentException("Non-negative skip must be used for pagination.");
                }

                items = items.Skip(skip.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogWarning("Search: Negative take {Take}", take);
                    throw new ArgumentException("Non-negative take must be used for pagination.");
                }

                items = items.Take(take.Value);
            }

            var results = await items.ToListAsync(ct);
            _logger.LogInformation("Exiting Search: userId={UserId}, returned={Returned}", userId, results.Count);
            return new GetPantryItemsResult { TotalCount = count, Items = items.Select(i => i.ToDto()) };
        }

        // does not save here for transactional purposes
        private async Task<int> UpdatePantryItemFood(CreateUpdatePantryItemRequestDto pantryItemDto)
        {
            if (pantryItemDto.Food == null)
            {
                _logger.LogWarning("UpdatePantryItemFood: Food is null");
                throw new ArgumentException("Food is required.");
            }

            int foodId;

            if (pantryItemDto.Food.Mode == AddFoodMode.Existing)
            {
                var food = (ExistingFoodReferenceDto)pantryItemDto.Food;
                foodId = food.Id;
                if (await _context.Foods.FirstOrDefaultAsync(i => i.Id == foodId) == null)
                {
                    _logger.LogWarning("UpdatePantryItemFood: FoodId {FoodId} not valid", foodId);
                    throw new ValidationException("FoodId provided was not valid.");
                }
            }
            else if (pantryItemDto.Food.Mode == AddFoodMode.New)
            {
                var food1 = (NewFoodReferenceDto)pantryItemDto.Food;
                if (await _context.Categories.FirstOrDefaultAsync(c => c.Id == food1.CategoryId) == null)
                {
                    _logger.LogWarning("UpdatePantryItemFood: CategoryId {CategoryId} not valid for new food", food1.CategoryId);
                    throw new ArgumentException("CategoryId is required on pantry items with new foods.");
                }

                foodId = CreateNewFood(food1);
            }
            else
            {
                _logger.LogError("UpdatePantryItemFood: Unknown food mode {Mode}", pantryItemDto.Food.Mode);
                throw new ArgumentException("FoodId or FoodName must be provided.");
            }

            return foodId;
        }

        // does not save here for transactional purposes
        private async Task<PantryItem> CreatePantryItem(CreateUpdatePantryItemRequestDto pantryItemDto, int userId)
        {
            if (pantryItemDto.Quantity < 0)
            {
                _logger.LogWarning("CreatePantryItem: Negative quantity {Quantity} for userId {UserId}", pantryItemDto.Quantity, userId);
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

            await _context.PantryItems.AddAsync(entity);

            _logger.LogInformation("CreatePantryItem: Created pantry item for userId={UserId}, itemId={ItemId}", userId, entity.Id);
            return entity;
        }

        // does not save here for transactional purposes
        private int CreateNewFood(NewFoodReferenceDto dto)
        {
            var food = new Food
            {
                Name = dto.Name!,
                CategoryId = dto.CategoryId
            };

            _context.Foods.Add(food);
            _logger.LogInformation("CreateNewFood: Created new food {FoodName} with id={FoodId}", food.Name, food.Id);
            return food.Id;
        }
    }
}