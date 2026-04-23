using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Persistence.Migrations;

public partial class AddOrderQuantityAndTotalValue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Quantity",
            table: "orders",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalValue",
            table: "orders",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("UPDATE orders SET \"TotalValue\" = \"Value\" * \"Quantity\";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Quantity",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "TotalValue",
            table: "orders");
    }
}

