using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PureFinalReviewFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_Customers_CustomerId1",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_Products_ProductId1",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_Users_CustomerId",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_CustomerId",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_ProductId1",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "ProductReviews");

            migrationBuilder.RenameColumn(
                name: "CustomerId1",
                table: "ProductReviews",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductReviews_CustomerId1",
                table: "ProductReviews",
                newName: "IX_ProductReviews_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_Users_UserId",
                table: "ProductReviews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_Users_UserId",
                table: "ProductReviews");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ProductReviews",
                newName: "CustomerId1");

            migrationBuilder.RenameIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews",
                newName: "IX_ProductReviews_CustomerId1");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "ProductReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "ProductReviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_CustomerId",
                table: "ProductReviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId1",
                table: "ProductReviews",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_Customers_CustomerId1",
                table: "ProductReviews",
                column: "CustomerId1",
                principalTable: "Customers",
                principalColumn: "CustomerID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_Products_ProductId1",
                table: "ProductReviews",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_Users_CustomerId",
                table: "ProductReviews",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "UserID");
        }
    }
}
