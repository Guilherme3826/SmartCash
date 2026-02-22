using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCash.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumIdProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_Consumivel_IdProduto",
                table: "Item");

            migrationBuilder.RenameColumn(
                name: "IdProduto",
                table: "Item",
                newName: "IdConsumivel");

            migrationBuilder.RenameIndex(
                name: "IX_Item_IdProduto",
                table: "Item",
                newName: "IX_Item_IdConsumivel");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Consumivel_IdConsumivel",
                table: "Item",
                column: "IdConsumivel",
                principalTable: "Consumivel",
                principalColumn: "IdProduto",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_Consumivel_IdConsumivel",
                table: "Item");

            migrationBuilder.RenameColumn(
                name: "IdConsumivel",
                table: "Item",
                newName: "IdProduto");

            migrationBuilder.RenameIndex(
                name: "IX_Item_IdConsumivel",
                table: "Item",
                newName: "IX_Item_IdProduto");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Consumivel_IdProduto",
                table: "Item",
                column: "IdProduto",
                principalTable: "Consumivel",
                principalColumn: "IdProduto",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
