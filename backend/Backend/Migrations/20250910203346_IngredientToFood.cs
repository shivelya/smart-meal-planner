using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class IngredientToFood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanEntries_Recipes_RecipeId",
                table: "MealPlanEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PantryItems_Ingredients_IngredientId",
                table: "PantryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Ingredients_IngredientId",
                table: "RecipeIngredients");

            migrationBuilder.RenameTable(
                name: "Ingredients",
                newName: "Foods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_PantryItems_UserId",
                table: "PantryItems");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "MealPlanEntries");

            migrationBuilder.RenameColumn(
                name: "IngredientId",
                table: "RecipeIngredients",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "IngredientId",
                table: "PantryItems",
                newName: "FoodId");

            migrationBuilder.RenameIndex(
                name: "IX_PantryItems_IngredientId",
                table: "PantryItems",
                newName: "IX_PantryItems_FoodId");

            migrationBuilder.AddColumn<int>(
                name: "FoodId",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "RecipeId",
                table: "MealPlanEntries",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "Cooked",
                table: "MealPlanEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MealPlanEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "FoodId" });

            // migrationBuilder.CreateTable(
            //     name: "Foods",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "integer", nullable: false)
            //             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //         Name = table.Column<string>(type: "text", nullable: false),
            //         CategoryId = table.Column<int>(type: "integer", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Foods", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_Foods_Categories_CategoryId",
            //             column: x => x.CategoryId,
            //             principalTable: "Categories",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_Title",
                table: "Recipes",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_FoodId",
                table: "RecipeIngredients",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_PantryItem_UserIdFoodId",
                table: "PantryItems",
                columns: new[] { "UserId", "FoodId" });

            migrationBuilder.CreateIndex(
                name: "IX_Food_Name",
                table: "Foods",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_CategoryId",
                table: "Foods",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanEntries_Recipes_RecipeId",
                table: "MealPlanEntries",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PantryItems_Foods_FoodId",
                table: "PantryItems",
                column: "FoodId",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Foods_FoodId",
                table: "RecipeIngredients",
                column: "FoodId",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanEntries_Recipes_RecipeId",
                table: "MealPlanEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PantryItems_Foods_FoodId",
                table: "PantryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Foods_FoodId",
                table: "RecipeIngredients");

            migrationBuilder.RenameTable(
                name: "Foods",
                newName: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Recipe_Title",
                table: "Recipes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_FoodId",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_PantryItem_UserIdFoodId",
                table: "PantryItems");

            migrationBuilder.DropColumn(
                name: "FoodId",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "Cooked",
                table: "MealPlanEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MealPlanEntries");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RecipeIngredients",
                newName: "IngredientId");

            migrationBuilder.RenameColumn(
                name: "FoodId",
                table: "PantryItems",
                newName: "IngredientId");

            migrationBuilder.RenameIndex(
                name: "IX_PantryItems_FoodId",
                table: "PantryItems",
                newName: "IX_PantryItems_IngredientId");

            migrationBuilder.AlterColumn<int>(
                name: "RecipeId",
                table: "MealPlanEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "MealPlanEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "IngredientId" });

            // migrationBuilder.CreateTable(
            //     name: "Ingredients",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "integer", nullable: false)
            //             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //         CategoryId = table.Column<int>(type: "integer", nullable: false),
            //         Name = table.Column<string>(type: "text", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Ingredients", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_Ingredients_Categories_CategoryId",
            //             column: x => x.CategoryId,
            //             principalTable: "Categories",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_UserId",
                table: "PantryItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_CategoryId",
                table: "Ingredients",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanEntries_Recipes_RecipeId",
                table: "MealPlanEntries",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PantryItems_Ingredients_IngredientId",
                table: "PantryItems",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Ingredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
