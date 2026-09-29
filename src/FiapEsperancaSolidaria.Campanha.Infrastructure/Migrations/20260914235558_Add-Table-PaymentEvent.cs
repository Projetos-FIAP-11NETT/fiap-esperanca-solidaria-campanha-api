using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTablePaymentEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentEvent",
                schema: "fundraising",
                columns: table => new
                {
                    PaymentEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DonationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentEventType = table.Column<byte>(type: "smallint", nullable: false),
                    Observation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEvent", x => x.PaymentEventId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentEvent",
                schema: "fundraising");
        }
    }
}
