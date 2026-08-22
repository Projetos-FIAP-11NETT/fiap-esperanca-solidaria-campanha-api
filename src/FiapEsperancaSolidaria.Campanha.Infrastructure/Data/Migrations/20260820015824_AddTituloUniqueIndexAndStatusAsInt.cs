using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTituloUniqueIndexAndStatusAsInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ALTER COLUMN TYPE simples falha se já existir alguma linha com o valor antigo
            // (texto "Ativa"/"Concluida"/"Cancelada" não é castável direto pra integer).
            migrationBuilder.Sql(
                """
                ALTER TABLE "Campanhas"
                ALTER COLUMN "Status" TYPE integer
                USING (CASE "Status"
                    WHEN 'Ativa' THEN 1
                    WHEN 'Concluida' THEN 2
                    WHEN 'Cancelada' THEN 3
                END);
                """);

            migrationBuilder.AddColumn<string>(
                name: "TituloNormalizado",
                table: "Campanhas",
                type: "text",
                nullable: true,
                computedColumnSql: "lower(\"Titulo\")",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_TituloNormalizado",
                table: "Campanhas",
                column: "TituloNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Campanhas_TituloNormalizado",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "TituloNormalizado",
                table: "Campanhas");

            migrationBuilder.Sql(
                """
                ALTER TABLE "Campanhas"
                ALTER COLUMN "Status" TYPE character varying(20)
                USING (CASE "Status"
                    WHEN 1 THEN 'Ativa'
                    WHEN 2 THEN 'Concluida'
                    WHEN 3 THEN 'Cancelada'
                END);
                """);
        }
    }
}
