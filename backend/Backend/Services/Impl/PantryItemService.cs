using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class PantryItemService : IPantryItemService
    {
        private readonly PlannerContext _context;
        private readonly ILogger<PantryItemService> _logger;
        public PantryItemService(PlannerContext plannerContext, ILogger<PantryItemService> logger)
        {
            _context = plannerContext;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new pantry item for the specified user. Assumes an IngredientId for ingredients that already exist,
        /// and assumes an IntredientName and CategoryId for ingredients that need to be added to the DB.
        /// </summary>
        /// <param name="pantryItemDto">The DTO containing pantry item details.</param>
        /// <param name="userId">The user ID to associate with the pantry item.</param>
        /// <returns>The created pantry item DTO.</returns>
        public async Task<PantryItemDto> CreatePantryItemAsync(CreatePantryItemDto pantryItemDto, int userId)
        {
            _logger.LogInformation("Creating for user {0} pantry item {1}", userId, pantryItemDto);

            int ingredientId;

            if (pantryItemDto.IngredientId.HasValue)
            {
                ingredientId = pantryItemDto.IngredientId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(pantryItemDto.IngredientName))
            {
                if (pantryItemDto.CategoryId == null)
                {
                    _logger.LogWarning("Ingredientname was provided with CategoryId");
                    throw new ArgumentException("CategoryId is required on pantry items with new ingredients.");
                }

                var ingredient = new Ingredient
                {
                    Name = pantryItemDto.IngredientName,
                    CategoryId = (int)pantryItemDto.CategoryId
                }; 

                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();
                ingredientId = ingredient.Id;
            }
            else
            {
                _logger.LogError("IntredientId or IngredientName must be provided.");
                throw new ArgumentException("IngredientId or IngredientName must be provided.");
            }

            var entity = new PantryItem
            {
                IngredientId = ingredientId,
                Quantity = pantryItemDto.Quantity,
                Unit = pantryItemDto.Unit,
                UserId = userId
            };

            _context.PantryItems.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pantry item created");

            return ToDto(entity);
        }

        /// <summary>
        /// Creates multiple pantry items for the specified user. Assumes an IngredientId for ingredients that alreay exist
        /// and assumes an IngredientName and CategoryId for ingredients that need to be added to the DB.
        /// </summary>
        /// <param name="pantryItemDtos">A collection of DTOs containing pantry item details.</param>
        /// <param name="userId">The user ID to associate with the pantry items.</param>
        /// <returns>A collection of created pantry item DTOs.</returns>
        public async Task<IEnumerable<PantryItemDto>> CreatePantryItemsAsync(IEnumerable<CreatePantryItemDto> pantryItemDtos, int userId)
        {
            _logger.LogInformation("Creating for user {0} {1} pantry items", userId, pantryItemDtos.Count());

            if (pantryItemDtos.Where(dto => dto.IngredientId == null && dto.IngredientName == null).Count() > 0)
            {
                _logger.LogWarning("Pantry items with no ingredient were added. These will be filtered out.");
            }

            var entities = pantryItemDtos.Where(dto => dto.IngredientId != null || (dto.IngredientName != null && dto.CategoryId != null))
                .Select(dto =>
                {
                    int ingredientId;
                    if (dto.IngredientId.HasValue)
                    {
                        ingredientId = dto.IngredientId.Value;
                    }
                    else if (!string.IsNullOrWhiteSpace(dto.IngredientName))
                    {
                        var ingredient = new Ingredient
                        {
                            Name = dto.IngredientName,
                            CategoryId = (int)dto.CategoryId!
                        };

                        _context.Ingredients.Add(ingredient);
                        _context.SaveChanges();
                        ingredientId = ingredient.Id;
                    }
                    else
                    {
                        // shouldn't get here based on the where statement above
                        _logger.LogError("IngredientId or IngredientName and CategoryId must be provided.");
                        throw new ArgumentException("IngredientId or IngredientName and CategoryId must be provided.");
                    }

                    return new PantryItem
                    {
                        IngredientId = ingredientId,
                        Quantity = dto.Quantity,
                        Unit = dto.Unit,
                        UserId = userId
                    };
                }).ToList();

            _context.PantryItems.AddRange(entities);
            int count = await _context.SaveChangesAsync();

            _logger.LogInformation("{0} pantry items created.", count);

            return entities.Select(ToDto);
        }

        /// <summary>
        /// Deletes a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <returns>True if the item was deleted, otherwise false.</returns>
        public async Task<bool> DeletePantryItemAsync(int id)
        {
            _logger.LogInformation("Deleting pantry item {0}", id);
            var entity = await _context.PantryItems.FindAsync(id);
            if (entity is null) return false;

            _context.PantryItems.Remove(entity);
            var deleted = await _context.SaveChangesAsync();
            _logger.LogInformation("{0} pantry items deleted", deleted);
            return deleted > 0;
        }

        /// <summary>
        /// Deletes multiple pantry items by their IDs.
        /// </summary>
        /// <param name="ids">A collection of pantry item IDs to delete.</param>
        /// <returns>The number of items deleted.</returns>
        public async Task<int> DeletePantryItemsAsync(IEnumerable<int> ids)
        {
            _logger.LogInformation("Deleting {0} pantry items", ids.Count());
            var entities = _context.PantryItems.Where(p => ids.Contains(p.Id));
            _context.PantryItems.RemoveRange(entities);
            int count = await _context.SaveChangesAsync();
            _logger.LogInformation("{0} pantry items deleted", count);
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
            _logger.LogInformation("Getting {0} pantry items, on page {1}", pageSize, pageNumber);
            var query = _context.PantryItems.AsQueryable();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Ingredient.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items.Select(ToDto), totalCount);
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
            return entity is null ? null : ToDto(entity);
        }

        /// <summary>
        /// Updates an existing pantry item.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to update.</param>
        /// <returns>The updated pantry item DTO.</returns>
        public Task<PantryItemDto> UpdatePantryItemAsync(PantryItemDto pantryItemDto)
        {
            throw new NotImplementedException();
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
                .Where(i => i.Ingredient.Name.ToLower().Contains(search.ToLower()))
                .OrderBy(i => i.Ingredient.Name)
                .Take(20) // limit results for performance
                .ToListAsync();

            return items.Select(ToDto);
        }

        private static PantryItemDto ToDto(PantryItem entity) => new PantryItemDto
        {
            Id = entity.Id,
            IngredientId = entity.IngredientId,
            Quantity = entity.Quantity,
            Unit = entity.Unit,
            UserId = entity.UserId
        };
    }
}