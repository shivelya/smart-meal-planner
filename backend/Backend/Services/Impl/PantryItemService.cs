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
        public async Task<PantryItemDto> CreatePantryItemAsync(CreatePantryItemDto pantryItemDto, int userId)
        {
            _logger.LogInformation("Creating for user {0} pantry item {1}", userId, pantryItemDto);

            int ingredientId;

            if (pantryItemDto is CreatePantryItemOldIngredientDto dto1)
            {
                ingredientId = dto1.IngredientId!;
                if (await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId) == null)
                {
                    _logger.LogWarning("IngredientId provided was not valid.");
                    throw new ValidationException("IngredientId provided was not valid.");
                }
            }
            else if (pantryItemDto is CreatePantryItemNewIngredientDto dto)
            {
                if (await _context.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId) == null)
                {
                    _logger.LogWarning("Ingredientname was provided without CategoryId");
                    throw new ArgumentException("CategoryId is required on pantry items with new ingredients.");
                }

                ingredientId = await CreateNewIngredient(dto);
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

            return entity.ToDto();
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
            _logger.LogInformation("Creating for user {id} {count} pantry items", userId, pantryItemDtos.Count());

            if (pantryItemDtos.Where(dto => !(dto is CreatePantryItemNewIngredientDto) && !(dto is CreatePantryItemOldIngredientDto)).Count() > 0)
            {
                _logger.LogWarning("Pantry items with no ingredient were added. These will be filtered out.");
                throw new ArgumentException("Pantry items with no ingredient were added. An ingredient is required.");
            }

            var pantryItems = pantryItemDtos.OfType<CreatePantryItemOldIngredientDto>()
                .Select(dto =>
                {
                    return new PantryItem
                    {
                        IngredientId = dto.IngredientId,
                        Quantity = dto.Quantity,
                        Unit = dto.Unit,
                        UserId = userId
                    };
                }).ToList();

            var tasks = pantryItemDtos.OfType<CreatePantryItemNewIngredientDto>()
                .Select(async dto =>
                {
                    if (string.IsNullOrWhiteSpace(dto.IngredientName))
                    {
                        _logger.LogWarning("Ingredient name is required for creating a new ingredniet.");
                        throw new ValidationException("Ingredient name is required for creating a new ingredient.");
                    }

                    var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
                    if (category == null)
                    {
                        _logger.LogWarning("A valid category is required for creating a new ingredient.");
                        throw new ValidationException("A valid category is required for creating anew ingredient.");
                    }

                    int ingredientId = await CreateNewIngredient(dto);

                    return new PantryItem
                    {
                        IngredientId = ingredientId,
                        Quantity = dto.Quantity,
                        Unit = dto.Unit,
                        UserId = userId
                    };
                }).ToList();

            pantryItems.AddRange(await Task.WhenAll(tasks));

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
                .OrderBy(p => p.Ingredient.Name)
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
        public async Task<PantryItemDto> UpdatePantryItemAsync(CreatePantryItemDto pantryItemDto, int userId)
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

            if (pantryItemDto is CreatePantryItemOldIngredientDto)
            {
                var dto = (CreatePantryItemOldIngredientDto)pantryItemDto;
                item.IngredientId = dto.IngredientId;
            }
            else if (pantryItemDto is CreatePantryItemNewIngredientDto dto)
            {
                if (string.IsNullOrWhiteSpace(dto.IngredientName))
                {
                    _logger.LogWarning("IngredientName must be supplied to create new Ingredient.");
                    throw new ArgumentException("IngredientName must supplied to create new Ingredient.");
                }

                if (await _context.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId) == null)
                {
                    _logger.LogWarning("A valid CategoryId must be supplied to create new Ingredient.");
                    throw new ArgumentException("A valid CategoryId must supplied to create new Ingredient.");
                }

                item.IngredientId = await CreateNewIngredient(dto);
            }

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
                .Where(i => i.Ingredient.Name.Contains(search.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(i => i.Ingredient.Name)
                .Take(20) // limit results for performance
                .ToListAsync();

            return items.Select(i => i.ToDto());
        }

        private async Task<int> CreateNewIngredient(CreatePantryItemNewIngredientDto dto)
        {
            var ingredient = new Ingredient
            {
                Name = dto.IngredientName,
                CategoryId = dto.CategoryId
            };

            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();
            return ingredient.Id;
        }
    }
}