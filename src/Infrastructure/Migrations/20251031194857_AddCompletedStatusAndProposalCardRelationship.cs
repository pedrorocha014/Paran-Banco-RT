using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedStatusAndProposalCardRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:proposal_status", "created,approved,denied,completed")
                .OldAnnotation("Npgsql:Enum:proposal_status", "created,approved,denied");

            migrationBuilder.AddColumn<Guid>(
                name: "ProposalId",
                table: "Cards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ProposalId",
                table: "Cards",
                column: "ProposalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Proposals_ProposalId",
                table: "Cards",
                column: "ProposalId",
                principalTable: "Proposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Proposals_ProposalId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ProposalId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ProposalId",
                table: "Cards");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:proposal_status", "created,approved,denied")
                .OldAnnotation("Npgsql:Enum:proposal_status", "created,approved,denied,completed");
        }
    }
}
