using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCash.Migrations
{
    /// <inheritdoc />
    public partial class AlterandoTabelaProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_Produto_IdProduto",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Produto_Categoria_IdCategoria",
                table: "Produto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Produto",
                table: "Produto");

            migrationBuilder.RenameTable(
                name: "Produto",
                newName: "Consumivel");

            migrationBuilder.RenameIndex(
                name: "IX_Produto_IdCategoria",
                table: "Consumivel",
                newName: "IX_Consumivel_IdCategoria");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Consumivel",
                table: "Consumivel",
                column: "IdProduto");

            migrationBuilder.AddForeignKey(
                name: "FK_Consumivel_Categoria_IdCategoria",
                table: "Consumivel",
                column: "IdCategoria",
                principalTable: "Categoria",
                principalColumn: "IdCategoria",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Consumivel_IdProduto",
                table: "Item",
                column: "IdProduto",
                principalTable: "Consumivel",
                principalColumn: "IdProduto",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consumivel_Categoria_IdCategoria",
                table: "Consumivel");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_Consumivel_IdProduto",
                table: "Item");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Consumivel",
                table: "Consumivel");

            migrationBuilder.RenameTable(
                name: "Consumivel",
                newName: "Produto");

            migrationBuilder.RenameIndex(
                name: "IX_Consumivel_IdCategoria",
                table: "Produto",
                newName: "IX_Produto_IdCategoria");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Produto",
                table: "Produto",
                column: "IdProduto");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Produto_IdProduto",
                table: "Item",
                column: "IdProduto",
                principalTable: "Produto",
                principalColumn: "IdProduto",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produto_Categoria_IdCategoria",
                table: "Produto",
                column: "IdCategoria",
                principalTable: "Categoria",
                principalColumn: "IdCategoria",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
