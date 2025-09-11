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

        public async Task GenerateAsync(GenerateShoppingListRequestDto request, int userId)
        {
            if (request == null)
            {
                // shouldn't get here, we check this in controller
                _logger.LogWarning("request object is required.");
                throw new ArgumentException("request object is required.");
            }

            var mealPlan = _context.MealPlans
                .AsNoTracking()
                .Include(m => m.Meals)
                .ThenInclude(m => m.Recipe)
                .ThenInclude(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .FirstOrDefault(m => m.Id == request.MealPlanId);

            if (mealPlan == null)
            {
                _logger.LogWarning("Valid meal plan id must be given.");
                throw new ArgumentException("Valid meal plan id must be given.");
            }

            if (mealPlan.UserId != userId)
            {
                _logger.LogWarning("User does not have permission to access meal plan.");
                throw new ValidationException("User does not have permission to access meal plan.");
            }

            var pantryItems = _context.PantryItems
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Include(p => p.Food)
                .ToList();

            var shoppingList = new Dictionary<int, Food>();

            foreach (var meal in mealPlan.Meals)
            {
                if (meal.Recipe == null || meal.Recipe.Ingredients == null)
                    continue;

                foreach (var ingredient in meal.Recipe.Ingredients)
                {
                    if (ingredient.Food == null)
                        continue;

                    var pantryItem = pantryItems.FirstOrDefault(p => p.FoodId == ingredient.FoodId);

                    // If pantry item exists then count it as available. We aren't sophisticated enough to handle partial use of pantry items yet.
                    if (pantryItem != null && pantryItem.Quantity > 0)
                        continue;

                    if (shoppingList.ContainsKey(ingredient.FoodId))
                        continue;

                    shoppingList[ingredient.FoodId] = ingredient.Food;
                }
            }

            if (!request.Restart)
            {
                var existingItems = _context.ShoppingListItems
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .Include(s => s.Food)
                    .ThenInclude(f => f!.Category)
                    .ToList();

                foreach (var item in shoppingList)
                {
                    if (existingItems.FirstOrDefault(e => e.Food?.Id == item.Key) != null)
                        continue;

                    var newItem = new ShoppingListItem
                    {
                        UserId = userId,
                        FoodId = item.Key
                    };

                    _context.ShoppingListItems.Add(newItem);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                var itemsToRemove = _context.ShoppingListItems
                    .Where(s => s.UserId == userId);

                _context.ShoppingListItems.RemoveRange(itemsToRemove);

                foreach (var food in shoppingList.Values)
                {
                    var newItem = new ShoppingListItem
                    {
                        UserId = userId,
                        FoodId = food.Id
                    };

                    _context.ShoppingListItems.Add(newItem);
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}