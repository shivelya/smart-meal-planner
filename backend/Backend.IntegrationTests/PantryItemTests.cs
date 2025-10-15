using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class PantryItemTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PantryItemTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task SearchPantryItems_NoMatches_ReturnsOk()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("api/pantryitem?query=xyznotfound");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchPantryItems_Finds_Items_CaseInsensitive_AndPartial()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("api/pantryitem?query=FRIED");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Contains("fried", result.Items.First().Food.Name.ToLower());
        }

        [Fact]
        public async Task GetPantryItems_Returns_Pantry_Items()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("api/pantryitem");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);

            // we seed with 3 so we assume that'll be returned
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count());

            // ensure full object is returned
            var bananas = result.Items.FirstOrDefault(p => p.Food.Name == "Bananas");
            Assert.NotNull(bananas);
            Assert.Equal(3, bananas.Quantity);
            Assert.Equal("bunches", bananas.Unit);

            var chicken = result.Items.FirstOrDefault(p => p.Food.Name == "Fried Chicken");
            Assert.NotNull(chicken);
            Assert.Equal(1, chicken.Quantity);
            Assert.Equal("bag", chicken.Unit);

            var squash = result.Items.FirstOrDefault(p => p.Food.Name == "Butternut Squash");
            Assert.NotNull(squash);
            Assert.Equal(12, squash.Quantity);
            Assert.Null(squash.Unit);
        }

        [Fact]
        public async Task GetPantryItems_Returns_pages_whenRequested()
        {
            await _factory.LoginAsync(_client);

            // get a food so we know its id
            var getResponse = await _client.GetAsync("api/food?query=butternut");
            getResponse.EnsureSuccessStatusCode();
            var recipeResult = await getResponse.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(recipeResult);
            var id = recipeResult.Items.First().Id;

            //seed 15 pantry items
            var pantryItems = new List<CreateUpdatePantryItemRequestDto>();
            for (int i = 0; i < 15; i++)
            {
                pantryItems.Add(new CreateUpdatePantryItemRequestDto
                {
                    Quantity = i,
                    Food = new ExistingFoodReferenceDto { Id = id }
                });
            }

            var response = await _client.PostAsJsonAsync("api/pantryitem/bulk", pantryItems);
            response.EnsureSuccessStatusCode();

            //ask for 5 at a time
            for (int i = 0; i < 21; i += 5)
            {
                getResponse = await _client.GetAsync($"api/pantryitem?skip={i}&take=5");

                getResponse.EnsureSuccessStatusCode();
                var result = await getResponse.Content.ReadFromJsonAsync<GetPantryItemsResult>();
                Assert.NotNull(result);
                Assert.Equal(18, result.TotalCount);

                //first 3 times it should return 5 items. 4th it should return 3 since we seed 3. Last time it should return zero but not error.
                if (i <= 10)
                    Assert.Equal(5, result.Items.Count());
                else if (i < 16)
                    Assert.Equal(3, result.Items.Count());
                else
                    Assert.Empty(result.Items);
            }
        }

        [Fact]
        public async Task GetPantryItems_Unauthorized_withNoToken()
        {
            var response = await _client.GetAsync("api/pantryitem");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData(-1, 10)]
        [InlineData(10, 0)]
        [InlineData(10, -1)]
        public async Task GetPantryItems_BadRequest_withBadValuesforSkipOrTake(int skip, int take)
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync($"api/pantryitem?query=banana&skip={skip}&take={take}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddPantryItem_CreatesNewPantryItem_withExistingFood()
        {
            await _factory.LoginAsync(_client);

            // look up food to get id
            var response = await _client.GetAsync("api/food?query=banana");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(result);
            var id = result.Items.First().Id;

            response = await _client.PostAsJsonAsync("api/pantryitem", new CreateUpdatePantryItemRequestDto
            {
                Quantity = 2,
                Unit = "box",
                Food = new ExistingFoodReferenceDto { Id = id }
            });

            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(newResult);
            Assert.Equal(id, newResult.FoodId);
            Assert.Equal(2, newResult.Quantity);
            Assert.Equal("box", newResult.Unit);
            Assert.Equal(result.Items.First().Name, newResult.Food.Name);
        }

        [Fact]
        public async Task AddPantryItem_CreatesNewPantryItem_withNewFood()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/pantryitem", new CreateUpdatePantryItemRequestDto
            {
                Quantity = 2,
                Unit = "box",
                Food = new NewFoodReferenceDto { CategoryId = 1, Name = "banana bread" }
            });

            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(newResult);
            Assert.NotEqual(0, newResult.FoodId);
            Assert.Equal(2, newResult.Quantity);
            Assert.Equal("box", newResult.Unit);
            Assert.Equal("banana bread", newResult.Food.Name);
            Assert.Equal(1, newResult.Food.CategoryId);
            Assert.NotNull(newResult.Food.Category);
        }

        [Fact]
        public async Task AddPantryItem_NegativeQuantity_BadRequest()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/pantryitem", new CreateUpdatePantryItemRequestDto
            {
                Quantity = -2,
                Unit = "box",
                Food = new NewFoodReferenceDto { CategoryId = 1, Name = "banana bread" }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddPantryItem_NullDTO_BadRequest()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync<CreateUpdatePantryItemRequestDto>("api/pantryitem", null!);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddPantryItem_NoFood_BadRequest()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/pantryitem", new CreateUpdatePantryItemRequestDto
            {
                Quantity = 2,
                Unit = "box",
                Food = null!
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddPantryItems_CreatesNewPantryItems_withExistingAndNewFood()
        {
            await _factory.LoginAsync(_client);

            // look up food to get id
            var response = await _client.GetAsync("api/food?query=banana");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(result);
            var id = result.Items.First().Id;

            response = await _client.PostAsJsonAsync("api/pantryitem/bulk", new List<CreateUpdatePantryItemRequestDto> { new() {
                Quantity = 2,
                Unit = "box",
                Food = new ExistingFoodReferenceDto { Id = id }
            }, new() {
                Quantity = 2,
                Unit = "box",
                Food = new NewFoodReferenceDto {  CategoryId = 1, Name = "dumplings" }
            }});

            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(newResult);
            Assert.Equal(2, newResult.TotalCount);
            Assert.Equal(2, newResult.Items.Count());

            Assert.NotNull(newResult.Items.FirstOrDefault(p => p.Food.Name.ToLower().Contains("banana")));
            Assert.NotNull(newResult.Items.FirstOrDefault(p => p.Food.Name.ToLower().Contains("dumplings")));
        }

        [Fact]
        public async Task AddPantryItems_NullOrEmptyArray_BadReqeust()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync<List<CreateUpdatePantryItemRequestDto>>("api/pantryitem/bulk", null!);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await _client.PostAsJsonAsync<List<CreateUpdatePantryItemRequestDto>>("api/pantryitem/bulk", []);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddPantryItems_CreatesNewPantryItems_SkippingBadItems()
        {
            await _factory.LoginAsync(_client);

            // look up food to get id
            var response = await _client.GetAsync("api/food?query=banana");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(result);
            var id = result.Items.First().Id;

            // null pantry item
            response = await _client.PostAsJsonAsync("api/pantryitem/bulk", new List<CreateUpdatePantryItemRequestDto> { new() {
                Quantity = 2,
                Unit = "box",
                Food = new ExistingFoodReferenceDto { Id = id }
            }, null!});

            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(newResult);
            Assert.Equal(1, newResult.TotalCount);
            Assert.Single(newResult.Items);
            Assert.NotNull(newResult.Items.FirstOrDefault(p => p.Food.Name.ToLower().Contains("banana")));

            // null food
            response = await _client.PostAsJsonAsync("api/pantryitem/bulk", new List<CreateUpdatePantryItemRequestDto> { new() {
                Quantity = 2,
                Unit = "box",
                Food = null! } });

            // it doesn't pass serialization so it gets a 400
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePantryItem_UpdatesPantryItem()
        {
            await _factory.LoginAsync(_client);

            // get a pantry item to update
            var response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            var item = result.Items.First();
            var id = item.Id;

            // update it
            response = await _client.PutAsJsonAsync($"api/pantryitem/{id}", new CreateUpdatePantryItemRequestDto
            {
                Quantity = item.Quantity + 10,
                Unit = "boxes",
                Food = new ExistingFoodReferenceDto { Id = item.FoodId }
            });

            response.EnsureSuccessStatusCode();
            var updatedResult = await response.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(updatedResult);
            Assert.Equal(id, updatedResult.Id);
            Assert.Equal(item.FoodId, updatedResult.FoodId);
            Assert.Equal(item.Quantity + 10, updatedResult.Quantity);
            Assert.Equal("boxes", updatedResult.Unit);
        }

        [Fact]
        public async Task UpdatePantryItem_BadRequests()
        {
            await _factory.LoginAsync(_client);

            // get a pantry item to update
            var response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            var item = result.Items.First();
            var id = item.Id;

            // null dto
            response = await _client.PutAsJsonAsync<CreateUpdatePantryItemRequestDto>($"api/pantryitem/{id}", null!);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // negative quantity
            response = await _client.PutAsJsonAsync($"api/pantryitem/{id}", new CreateUpdatePantryItemRequestDto
            {
                Quantity = -10,
                Unit = "boxes",
                Food = new ExistingFoodReferenceDto { Id = item.FoodId }
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // no food
            response = await _client.PutAsJsonAsync($"api/pantryitem/{id}", new CreateUpdatePantryItemRequestDto
            {
                Quantity = 10,
                Unit = "boxes",
                Food = null!
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // non-existent pantry item
            response = await _client.PutAsJsonAsync($"api/pantryitem/9999", new CreateUpdatePantryItemRequestDto
            {
                Quantity = 10,
                Unit = "boxes",
                Food = new ExistingFoodReferenceDto { Id = item.FoodId }
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePantryItem_Unauthorized_withNoToken()
        {
            var response = await _client.PutAsJsonAsync($"api/pantryitem/1", new CreateUpdatePantryItemRequestDto
            {
                Quantity = 10,
                Unit = "boxes",
                Food = new ExistingFoodReferenceDto { Id = 1 }
            });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeletePantryItem_DeletesPantryItem()
        {
            await _factory.LoginAsync(_client);

            // get a pantry item to delete
            var response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            var item = result.Items.First();
            var id = item.Id;

            // delete it
            response = await _client.DeleteAsync($"api/pantryitem/{id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // ensure it's gone
            response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(newResult);
            Assert.Equal(result.TotalCount - 1, newResult.TotalCount);
            Assert.Null(newResult.Items.FirstOrDefault(i => i.Id == id));
        }

        [Fact]
        public async Task DeletePantryItem_BadRequests()
        {
            await _factory.LoginAsync(_client);

            // get a pantry item to delete
            var response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            var item = result.Items.First();
            var id = item.Id;

            // non-existent id
            response = await _client.DeleteAsync($"api/pantryitem/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // invalid id
            response = await _client.DeleteAsync($"api/pantryitem/abc");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePantryItem_Unauthorized_withNoToken()
        {
            var response = await _client.DeleteAsync("api/pantryitem/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeletePantryItems_BulkDeletePantryItems()
        {
            await _factory.LoginAsync(_client);

            // get pantry items to delete
            var response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            var items = result.Items.Take(2).ToList();
            var ids = items.Select(i => i.Id).ToList();

            // delete them
            response = await _client.PostAsJsonAsync("api/pantryitem/bulk-delete", new DeleteRequest { Ids = ids });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // ensure they're gone
            response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(newResult);
            Assert.Equal(result.TotalCount - 2, newResult.TotalCount);
            foreach (var id in ids)
                Assert.Null(newResult.Items.FirstOrDefault(i => i.Id == id));
        }

        [Fact]
        public async Task DeletePantryItems_BadRequests()
        {
            await _factory.LoginAsync(_client);

            // get pantry items to delete
            var response = await _client.GetAsync("api/pantryitem");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            var items = result.Items.Take(2).ToList();
            var ids = items.Select(i => i.Id).ToList();

            // non-existent id
            var badIds = new List<int>(ids) { 9999 };
            response = await _client.PostAsJsonAsync("api/pantryitem/bulk-delete", badIds);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // invalid id - this gets caught in model binding so it returns a 400
            var responseMessage = new HttpRequestMessage(HttpMethod.Post, "api/pantryitem/bulk-delete")
            {
                Content = JsonContent.Create(new List<string> { "abc" })
            };
            response = await _client.SendAsync(responseMessage);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // empty array
            response = await _client.PostAsJsonAsync("api/pantryitem/bulk-delete", new List<int>());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // null array
            response = await _client.PostAsJsonAsync<List<int>>("api/pantryitem/bulk-delete", null!);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}