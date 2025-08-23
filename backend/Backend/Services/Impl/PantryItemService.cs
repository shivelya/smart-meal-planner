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
        /// Creates a new pantry item for the specified user. Assumes a new ingredient object has already been added.
        /// </summary>
        /// <param name="pantryItemDto">The DTO containing pantry item details.</param>
        /// <param name="userId">The user ID to associate with the pantry item.</param>
        /// <returns>The created pantry item DTO.</returns>
        public async Task<PantryItemDto> CreatePantryItemAsync(CreatePantryItemDto pantryItemDto, int userId)
        {
            _logger.LogInformation("Creating for user {0} pantry item {1}", userId, pantryItemDto);
            var entity = new PantryItem
            {
                IngredientId = pantryItemDto.IngredientId,
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
        /// Creates multiple pantry items for the specified user. Assumes the corresponding Ingredient objects have already been created.
        /// </summary>
        /// <param name="pantryItemDtos">A collection of DTOs containing pantry item details.</param>
        /// <param name="userId">The user ID to associate with the pantry items.</param>
        /// <returns>A collection of created pantry item DTOs.</returns>
        public async Task<IEnumerable<PantryItemDto>> CreatePantryItemsAsync(IEnumerable<CreatePantryItemDto> pantryItemDtos, int userId)
        {
            _logger.LogInformation("Creating for user {0} {1} pantry items", userId, pantryItemDtos.Count());
            var entities = pantryItemDtos.Select(dto => new PantryItem
            {
                IngredientId = dto.IngredientId,
                Quantity = dto.Quantity,
                Unit = dto.Unit,
                UserId = userId
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