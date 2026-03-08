using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryoablationSoftware.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseItemCodeLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("17ca1ef8-df41-4b28-a434-7b3bbcddd37f"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("7209baf8-49c7-44d7-8c45-ee07978d5ac4"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("a751a9e7-f36d-44b1-990c-579a662a865c"));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("0b003add-9ccf-432c-8681-b23a6e123465"), null, "Admin", "ADMIN" },
                    { new Guid("7c216fa8-0388-4b5f-91a8-26320d192d3f"), null, "Manager", "MANAGER" },
                    { new Guid("f257b5ff-bada-4c8d-a069-a1e54547ac99"), null, "Super Admin", "SUPER ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("0b003add-9ccf-432c-8681-b23a6e123465"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("7c216fa8-0388-4b5f-91a8-26320d192d3f"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("f257b5ff-bada-4c8d-a069-a1e54547ac99"));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("17ca1ef8-df41-4b28-a434-7b3bbcddd37f"), null, "Admin", "ADMIN" },
                    { new Guid("7209baf8-49c7-44d7-8c45-ee07978d5ac4"), null, "Manager", "MANAGER" },
                    { new Guid("a751a9e7-f36d-44b1-990c-579a662a865c"), null, "Super Admin", "SUPER ADMIN" }
                });
        }
    }
}
