using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiffinOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToDriverPayoutPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "DriverPayoutPolicies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayoutPolicies_CreatedBy",
                table: "DriverPayoutPolicies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayoutPolicies_UpdatedBy",
                table: "DriverPayoutPolicies",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverPayoutPolicies_Users_CreatedBy",
                table: "DriverPayoutPolicies",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverPayoutPolicies_Users_UpdatedBy",
                table: "DriverPayoutPolicies",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverPayoutPolicies_Users_CreatedBy",
                table: "DriverPayoutPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverPayoutPolicies_Users_UpdatedBy",
                table: "DriverPayoutPolicies");

            migrationBuilder.DropIndex(
                name: "IX_DriverPayoutPolicies_CreatedBy",
                table: "DriverPayoutPolicies");

            migrationBuilder.DropIndex(
                name: "IX_DriverPayoutPolicies_UpdatedBy",
                table: "DriverPayoutPolicies");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DriverPayoutPolicies");
        }
    }
}
