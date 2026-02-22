using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCash.Migrations
{
    /// <inheritdoc />
    public partial class AlterandoColumnIdProdutoFromConsumiveis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdProduto",
                table: "Consumivel",
                newName: "IdConsumivel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdConsumivel",
                table: "Consumivel",
                newName: "IdProduto");
        }
    }
}
