using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uis.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceWithExecutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExecutorId",
                table: "Services",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ExecutorId",
                table: "Services",
                column: "ExecutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Users_ExecutorId",
                table: "Services",
                column: "ExecutorId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_Users_ExecutorId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ExecutorId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ExecutorId",
                table: "Services");
        }
    }
}
