using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderManagement.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Customer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_orders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "processed_messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_processed_messages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "order_status_history",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                PreviousStatus = table.Column<int>(type: "integer", nullable: true),
                NewStatus = table.Column<int>(type: "integer", nullable: false),
                ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_order_status_history", x => x.Id);
                table.ForeignKey(
                    name: "FK_order_status_history_orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_order_status_history_OrderId_ChangedAt",
            table: "order_status_history",
            columns: new[] { "OrderId", "ChangedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_processed_messages_MessageId",
            table: "processed_messages",
            column: "MessageId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "order_status_history");
        migrationBuilder.DropTable(name: "processed_messages");
        migrationBuilder.DropTable(name: "orders");
    }
}

