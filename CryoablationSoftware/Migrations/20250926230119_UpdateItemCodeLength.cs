using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryoablationSoftware.Migrations
{
    /// <inheritdoc />
    public partial class UpdateItemCodeLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
              name: "ItemCode",
              table: "Items",
              type: "nvarchar(500)",
              maxLength: 500,
              nullable: false,
              oldClrType: typeof(string),
              oldType: "nvarchar(50)",
              oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                 name: "ItemCode",
                 table: "Items",
                 type: "nvarchar(50)",
                 maxLength: 50,
                 nullable: false,
                 oldClrType: typeof(string),
                 oldType: "nvarchar(500)",
                 oldMaxLength: 500);
        }
    }
}
