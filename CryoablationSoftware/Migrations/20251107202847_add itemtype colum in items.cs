using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryoablationSoftware.Migrations
{
    /// <inheritdoc />
    public partial class additemtypecoluminitems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("1b10290b-9029-46b5-a5b2-058bafe9ac6c"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("4328f95a-afdd-4dbc-b6ab-6e7b833bbac7"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("49c8773e-3462-4445-b9a1-d1d814c842c4"));

            migrationBuilder.AddColumn<string>(
         name: "ItemType",
         table: "Items",
         type: "nvarchar(20)",
         maxLength: 20,
         nullable: false,
         defaultValue: "Probe"); // Default value for existing rows

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("3f0343bd-9839-4937-98f9-01bde1bf435f"), null, "Super Admin", "SUPER ADMIN" },
                    { new Guid("95cd09a5-b2c0-48fe-b1c7-2f495daf93cb"), null, "Manager", "MANAGER" },
                    { new Guid("c426b6e7-1226-4920-9084-9d7d1ae6bd47"), null, "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("3f0343bd-9839-4937-98f9-01bde1bf435f"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("95cd09a5-b2c0-48fe-b1c7-2f495daf93cb"));

            migrationBuilder.DeleteData(
                table: "IdentityRole<Guid>",
                keyColumn: "Id",
                keyValue: new Guid("c426b6e7-1226-4920-9084-9d7d1ae6bd47"));

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "Items");

            migrationBuilder.InsertData(
                table: "IdentityRole<Guid>",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("1b10290b-9029-46b5-a5b2-058bafe9ac6c"), null, "Manager", "MANAGER" },
                    { new Guid("4328f95a-afdd-4dbc-b6ab-6e7b833bbac7"), null, "Admin", "ADMIN" },
                    { new Guid("49c8773e-3462-4445-b9a1-d1d814c842c4"), null, "Super Admin", "SUPER ADMIN" }
                });
        }
    }
}
