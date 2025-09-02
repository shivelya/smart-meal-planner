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

        /// <summary>
        /// Creates a new pantry item for the specified user. Assumes an IngredientId for ingredients that already exist,
        /// and assumes an IntredientName and CategoryId for ingredients that need to be added to the DB.
        /// </summary>
        /// <param name="pantryItemDto">The DTO containing pantry item details.</param>
        /// <param name="userId">The user ID to associate with the pantry item.</param>
        /// <returns>The created pantry item DTO.</returns>
        public async Task<PantryItemDto> CreatePantryItemAsync(PantryItemRequestDto pantryItemDto, int userId)
        {
            _logger.LogInformation("Creating for user {0} pantry item {1}", userId, pantryItemDto);
            PantryItem entity = await CreatePantryItem(pantryItemDto, userId);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pantry item created");

            return entity.ToDto();
        }

        /// <summary>
        /// Creates multiple pantry items for the specified user. Assumes an IngredientId for ingredients that alreay exist
        /// and assumes an IngredientName and CategoryId for ingredients that need to be added to the DB.
        /// </summary>
        /// <param name="pantryItemDtos">A collection of DTOs containing pantry item details.</param>
        /// <param name="userId">The user ID to associate with the pantry items.</param>
        /// <returns>A collection of created pantry item DTOs.</returns>
        public async Task<IEnumerable<PantryItemDto>> CreatePantryItemsAsync(IEnumerable<PantryItemRequestDto> pantryItemDtos, int userId)
        {
            _logger.LogInformation("Creating for user {id} {count} pantry items", userId, pantryItemDtos.Count());

            if (pantryItemDtos.Any(dto => dto.Food == null))
            {
                _logger.LogWarning("Pantry items with no ingredient were added. These will be filtered out.");
                throw new ArgumentException("Pantry items with no ingredient were added. An ingredient is required.");
            }

            var tasks = pantryItemDtos.Select(async dto => await CreatePantryItem(dto, userId));
            var pantryItems = await Task.WhenAll(tasks);

            _context.PantryItems.AddRange(pantryItems);
            int count = await _context.SaveChangesAsync();

            _logger.LogInformation("{count} pantry items created.", count);

            return pantryItems.Select(i => i.ToDto());
        }

        /// <summary>
        /// Deletes a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <returns>True if the item was deleted, otherwise false.</returns>
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

        /// <summary>
        /// Deletes multiple pantry items by their IDs.
        /// </summary>
        /// <param name="ids">A collection of pantry item IDs to delete.</param>
        /// <returns>The number of items deleted.</returns>
        public async Task<int> DeletePantryItemsAsync(IEnumerable<int> ids)
        {
            _logger.LogInformation("Deleting {count} pantry items", ids.Count());
            var entities = _context.PantryItems.Where(p => ids.Contains(p.Id));
            _context.PantryItems.RemoveRange(entities);
            int count = await _context.SaveChangesAsync();
            _logger.LogInformation("{count} pantry items deleted", count);
            return count;
        }

        /// <summary>
        /// Retrieves all pantry items with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the items and the total count.</returns>
        public async Task<(IEnumerable<PantryItemDto> Items, int TotalCount)> GetAllPantryItemsAsync(int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting {pageSize} pantry items, on page {page}", pageSize, pageNumber);
            var query = _context.PantryItems.AsQueryable();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Food.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items.Select(i => i.ToDto()), totalCount);
        }

        /// <summary>
        /// Retrieves a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <returns>The pantry item DTO if found, otherwise null.</returns>
        public async Task<PantryItemDto?> GetPantryItemByIdAsync(int id)
        {
            _logger.LogInformation("Getting pantry item {0}", id);
            var entity = await _context.PantryItems.FindAsync(id);
            return entity?.ToDto();
        }

        /// <summary>
        /// Updates an existing pantry item.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to update.</param>
        /// <param name="userId">The user id the item belongs to.</param>
        /// <returns>The updated pantry item DTO.</returns>
        public async Task<PantryItemDto> UpdatePantryItemAsync(PantryItemRequestDto pantryItemDto, int userId)
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

            var item = _context.PantryItems.FirstOrDefault(item => item.Id == pantryItemDto.Id);
            if (item == null)
            {
                _logger.LogWarning("Could not find pantry item {0} to update.", pantryItemDto.Id);
                throw new ArgumentException("Could not find pantry item {0} to update.", pantryItemDto.Id.ToString());
            }

            if (userId != item.UserId)
            {
                _logger.LogWarning("Cannot update pantry items for other users.");
                throw new ArgumentException("Cannot update pantry items for other users.");
            }

            item.Quantity = pantryItemDto.Quantity;
            item.Unit = pantryItemDto.Unit;
            item.FoodId = await UpdatePantryItemingredient(pantryItemDto);

            await _context.SaveChangesAsync();
            return item.ToDto();
        }

        /// <summary>
        /// Search for pantry items for the current user whose name matches the given search string.
        /// </summary>
        /// <param name="search">The string to search on.</param>
        /// <param name="userId">The id of the current user.</param>
        /// <returns>The pantry items which are a match.</returns>
        public async Task<IEnumerable<PantryItemDto>> Search(string search, int userId)
        {
            var items = await _context.PantryItems
                .Where(i => i.UserId == userId)
                .Where(i => i.Food.Name.Contains(search.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(i => i.Food.Name)
                .Take(20) // limit results for performance
                .ToListAsync();

            return items.Select(i => i.ToDto());
        }

        private async Task<int> UpdatePantryItemingredient(PantryItemRequestDto pantryItemDto)
        {
            int foodId;

            if (pantryItemDto.Food.Mode == AddFoodMode.Existing)
            {
                foodId = pantryItemDto.Food.Id!.Value;
                if (await _context.Foods.FirstOrDefaultAsync(i => i.Id == foodId) == null)
                {
                    _logger.LogWarning("FoodId provided was not valid.");
                    throw new ValidationException("FoodId provided was not valid.");
                }
            }
            else if (pantryItemDto.Food.Mode == AddFoodMode.New)
            {
                if (await _context.Categories.FirstOrDefaultAsync(c => c.Id == pantryItemDto.Food.CategoryId) == null)
                {
                    _logger.LogWarning("Ingredientname was provided without CategoryId");
                    throw new ArgumentException("CategoryId is required on pantry items with new ingredients.");
                }

                foodId = await CreateNewIngredient(pantryItemDto.Food);
            }
            else
            {
                _logger.LogError("IntredientId or IngredientName must be provided.");
                throw new ArgumentException("IngredientId or IngredientName must be provided.");
            }

            return foodId;
        }

        private async Task<PantryItem> CreatePantryItem(PantryItemRequestDto pantryItemDto, int userId)
        {
            int ingredientId = await UpdatePantryItemingredient(pantryItemDto);

            var entity = new PantryItem
            {
                FoodId = ingredientId,
                Quantity = pantryItemDto.Quantity,
                Unit = pantryItemDto.Unit,
                UserId = userId
            };

            _context.PantryItems.Add(entity);
            return entity;
        }

        private async Task<int> CreateNewIngredient(FoodReferenceDto dto)
        {
            var ingredient = new Food
            {
                Name = dto.Name!,
                CategoryId = dto.CategoryId!.Value
            };

            _context.Foods.Add(ingredient);
            await _context.SaveChangesAsync();
            return ingredient.Id;
        }
    }
}