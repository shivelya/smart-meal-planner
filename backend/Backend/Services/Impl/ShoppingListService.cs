using System.ComponentModel.DataAnnotations;
using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class ShoppingListService(PlannerContext context, ILogger<ShoppingListService> logger) : IShoppingListService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<ShoppingListService> _logger = logger;

        public async Task<GetShoppingListResult> GetShoppingListAsync(int userId)
        {
            _logger.LogInformation("Entering GetShoppingListAsync: userId={UserId}", userId);
            // sorts by category first, with items with no category at the end, then by food name
            var items = await _context.ShoppingListItems
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Include(s => s.Food)
                .ThenInclude(f => f!.Category)
                .OrderBy(i => i.Food == null ? 1 : 0)
                .ThenBy(i => i.Food == null ? "Other" : i.Food.Category.Name)
                .ThenBy(i => i.Food == null ? i.Notes : i.Food.Name)
                .ToListAsync();

            _logger.LogInformation("GetShoppingListAsync: Retrieved {Count} items for userId={UserId}", items.Count, userId);
            _logger.LogInformation("Exiting GetShoppingListAsync: userId={UserId}", userId);
            return new GetShoppingListResult
            {
                TotalCount = items.Count,
                Foods = items.Select(i => i.ToDto())
            };
        }

        public async Task<ShoppingListItemDto> UpdateShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId)
        {
            _logger.LogInformation("Entering UpdateShoppingListItemAsync: userId={UserId}, itemId={ItemId}", userId, request?.Id);
            if (request == null)
            {
                // shouldn't get here, we check this in controller
                _logger.LogWarning("UpdateShoppingListItemAsync: request object is required.");
                throw new ArgumentException("request object is required.");
            }

            if (request.Id == null)
            {
                _logger.LogWarning("UpdateShoppingListItemAsync: Id is required for updating a shopping list item.");
                throw new ArgumentException("Id is required for updating a shopping list item.");
            }

            if (await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) == null)
                throw new ValidationException("User not found.");

            var item = await _context.ShoppingListItems.FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId);

            if (item == null)
            {
                _logger.LogWarning("UpdateShoppingListItemAsync: Shopping list item not found.");
                throw new ValidationException("Shopping list item not found.");
            }

            if (request.FoodId != null)
            {
                var food = await _context.Foods
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == request.FoodId);

                if (food == null)
                {
                    _logger.LogWarning("UpdateShoppingListItemAsync: Valid food id must be given.");
                    throw new ArgumentException("Valid food id must be given.");
                }

                if (food.Id != item.FoodId)
                    item.Food = food;
            }

            item.FoodId = request.FoodId;
            item.Purchased = request.Purchased;
            item.Notes = request.Notes;

            await _context.SaveChangesAsync();
            _logger.LogInformation("UpdateShoppingListItemAsync: Updated item for userId={UserId}, itemId={ItemId}", userId, item.Id);
            _logger.LogInformation("Exiting UpdateShoppingListItemAsync: userId={UserId}, itemId={ItemId}", userId, item.Id);
            return item.ToDto();
        }

        public async Task<ShoppingListItemDto> AddShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId)
        {
            _logger.LogInformation("Entering AddShoppingListItemAsync: userId={UserId}", userId);
            if (request == null)
            {
                // shouldn't get here, we check this in controller
                _logger.LogWarning("AddShoppingListItemAsync: request object is required.");
                throw new ArgumentException("request object is required.");
            }

            if (request.Id != null)
            {
                _logger.LogWarning("AddShoppingListItemAsync: Id is not allowed for creating a shopping list item.");
                throw new ArgumentException("Id is not allowed for creating a shopping list item.");
            }

            if (await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId) == null)
                throw new ValidationException("User not found.");

            if (request.FoodId != null)
            {
                var food = await _context.Foods
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == request.FoodId);

                if (food == null)
                {
                    _logger.LogWarning("AddShoppingListItemAsync: If food it is given, it must be valid.");
                    throw new ArgumentException("Valid food id must be given.");
                }
            }

            var item = new ShoppingListItem
            {
                UserId = userId,
                FoodId = request.FoodId,
                Purchased = request.Purchased,
                Notes = request.Notes
            };

            await _context.ShoppingListItems.AddAsync(item);
            await _context.SaveChangesAsync();
            _logger.LogInformation("AddShoppingListItemAsync: Added item for userId={UserId}, itemId={ItemId}", userId, item.Id);
            _logger.LogInformation("Exiting AddShoppingListItemAsync: userId={UserId}, itemId={ItemId}", userId, item.Id);
            return item.ToDto();
        }

        public async Task<bool> DeleteShoppingListItemAsync(int id, int userId)
        {
            _logger.LogInformation("Entering DeleteShoppingListItemAsync: userId={UserId}, itemId={ItemId}", userId, id);
            if (id <= 0)
            {
                // shouldn't get here, we check this in controller
                _logger.LogWarning("DeleteShoppingListItemAsync: Valid id is required.");
                throw new ArgumentException("Valid id is required.");
            }

            var item = _context.ShoppingListItems
                .FirstOrDefault(s => s.Id == id && s.UserId == userId);

            if (item == null)
            {
                _logger.LogWarning("DeleteShoppingListItemAsync: Shopping list item not found.");
                throw new ValidationException("Shopping list item not found.");
            }

            _context.ShoppingListItems.Remove(item);
            var result = await _context.SaveChangesAsync() > 0;
            _logger.LogInformation("DeleteShoppingListItemAsync: Deleted item for userId={UserId}, itemId={ItemId}", userId, id);
            _logger.LogInformation("Exiting DeleteShoppingListItemAsync: userId={UserId}, itemId={ItemId}", userId, id);
            return result;
        }

        public async Task GenerateAsync(GenerateShoppingListRequestDto request, int userId)
        {
            _logger.LogInformation("Entering GenerateAsync: userId={UserId}, mealPlanId={MealPlanId}, restart={Restart}", userId, request?.MealPlanId, request?.Restart);
            if (request == null)
            {
                // shouldn't get here, we check this in controller
                _logger.LogWarning("GenerateAsync: request object is required.");
                throw new ArgumentException("request object is required.");
            }

            // get whole meal plan object with meals, recipes, ingredients and foods
            // so we can build the shopping list with the foods
            var mealPlan = await _context.MealPlans
                .AsNoTracking()
                .Include(m => m.Meals)
                .ThenInclude(m => m.Recipe)
                .ThenInclude(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .FirstOrDefaultAsync(m => m.Id == request.MealPlanId && m.UserId == userId);

            if (mealPlan == null)
            {
                _logger.LogWarning("GenerateAsync: Valid meal plan id must be given.");
                throw new ArgumentException("Valid meal plan id must be given.");
            }

            var shoppingList = await BuildShoppingListAsync(mealPlan, userId);

            // if restart is false, we add to existing shopping list, otherwise we clear it and start again
            if (!request.Restart)
                await AppendShoppingListAsync(userId, shoppingList);
            else
                await RestartShoppingListAsync(userId, shoppingList);

            await _context.SaveChangesAsync();

            // we don't return the shopping list here, the client can call GetShoppingList to get it if needed
            _logger.LogInformation("GenerateAsync: Generated shopping list for user {UserId} from meal plan {MealPlanId} (restart={Restart})", userId, request.MealPlanId, request.Restart);
            _logger.LogInformation("Exiting GenerateAsync: userId={UserId}, mealPlanId={MealPlanId}, restart={Restart}", userId, request.MealPlanId, request.Restart);
        }

        private async Task<Dictionary<int, Food>> BuildShoppingListAsync(MealPlan mealPlan, int userId)
        {
            _logger.LogInformation("Entering BuildShoppingListAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, mealPlan?.Id);
            // get pantry items so we can exclude foods already in pantry from shopping list
            // include food so we can reference it when building the shopping list
            var pantryItems = _context.PantryItems
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Include(p => p.Food)
                .AsQueryable();

            // loop through every meal
            var shoppingList = new Dictionary<int, Food>();
            foreach (var meal in mealPlan!.Meals) // mealPlan is not null here because we checked it in the calling method
            {
                // if theres no recipe or ingredients, skip it
                if (meal.Recipe == null || meal.Recipe.Ingredients == null)
                    continue;

                // get the food ids needed for this meal
                var neededFoodIds = meal.Recipe.Ingredients.Select(i => i.FoodId).ToList();

                // check which of these foods are already in the pantry - trying to minimize what we pull from the database
                var mealPantryItems = await pantryItems.Where(p => neededFoodIds.Contains(p.FoodId)).ToListAsync();
                if (mealPantryItems.Count == neededFoodIds.Count)
                    continue;

                // remove foods already in pantry from needed food ids
                neededFoodIds.RemoveAll(id => mealPantryItems.Any(p => p.FoodId == id));
                if (neededFoodIds.Count == 0)
                    continue;

                // add the remaining needed foods to the shopping list
                shoppingList = shoppingList.Concat(
                    meal.Recipe.Ingredients
                        .Where(i => neededFoodIds.Contains(i.FoodId))
                        .ToDictionary(i => i.FoodId, i => i.Food)
                ).ToDictionary(k => k.Key, v => v.Value);
            }

            _logger.LogInformation("BuildShoppingListAsync: Built shopping list with {Count} items for userId={UserId}", shoppingList.Count, userId);
            _logger.LogInformation("Exiting BuildShoppingListAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, mealPlan?.Id);
            return shoppingList;
        }

        // don't save here for transactional purposes
        private async Task AppendShoppingListAsync(int userId, Dictionary<int, Food> shoppingList)
        {
            _logger.LogInformation("Entering AppendShoppingListAsync: userId={UserId}, itemCount={ItemCount}", userId, shoppingList.Count);
            // get existing shopping list items for this user that match the foods in the new shopping list
            // include food so we can reference it when building the shopping list
            var existingItems = await _context.ShoppingListItems
                .AsNoTracking()
                .Include(s => s.Food)
                .Where(s => s.UserId == userId && s.FoodId != null && shoppingList.Keys.Contains(s.FoodId ?? 0))
                .Select(s => s.FoodId!.Value) // we know FoodId is not null here because of the where clause
                .ToListAsync();

            // only add items that aren't already in the shopping list
            // we do this in memory for simplicity, assuming shopping lists won't be huge
            var toAdd = shoppingList.Where(l => !existingItems.Contains(l.Key)).ToList();
            await _context.ShoppingListItems.AddRangeAsync(toAdd.Select(i => new ShoppingListItem
            {
                UserId = userId,
                FoodId = i.Key
            }));

            _logger.LogInformation("AppendShoppingListAsync: Appended {Count} items for userId={UserId}", toAdd.Count, userId);
            _logger.LogInformation("Exiting AppendShoppingListAsync: userId={UserId}, itemCount={ItemCount}", userId, shoppingList.Count);
        }

        // don't save here for transactional purposes
        private async Task RestartShoppingListAsync(int userId, Dictionary<int, Food> shoppingList)
        {
            _logger.LogInformation("Entering RestartShoppingListAsync: userId={UserId}, itemCount={ItemCount}", userId, shoppingList.Count);
            // remove all existing shopping list items for this user. We assume it's not the longest list
            // (especially with the where clause) so doing it in memory is ok for now
            var itemsToRemove = _context.ShoppingListItems
                                .Where(s => s.UserId == userId);
            _context.ShoppingListItems.RemoveRange(itemsToRemove);

            // add all the new items
            await _context.ShoppingListItems.AddRangeAsync(shoppingList.Values.Select(f => new ShoppingListItem
            {
                UserId = userId,
                FoodId = f.Id
            }));

            _logger.LogInformation("RestartShoppingListAsync: Restarted shopping list with {Count} items for userId={UserId}", shoppingList.Count, userId);
            _logger.LogInformation("Exiting RestartShoppingListAsync: userId={UserId}, itemCount={ItemCount}", userId, shoppingList.Count);
        }
    }
}