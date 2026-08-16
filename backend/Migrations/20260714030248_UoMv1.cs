using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class UoMv1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UoMCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaseUoMId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UoMCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UoMs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Synonyms = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UoMs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UoMs_UoMCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "UoMCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UoMConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    FromUoMId = table.Column<int>(type: "int", nullable: false),
                    ToUoMId = table.Column<int>(type: "int", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UoMConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UoMConversions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UoMConversions_UoMs_FromUoMId",
                        column: x => x.FromUoMId,
                        principalTable: "UoMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UoMConversions_UoMs_ToUoMId",
                        column: x => x.ToUoMId,
                        principalTable: "UoMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UoMCategories_BaseUoMId",
                table: "UoMCategories",
                column: "BaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_UoMCategories_Code",
                table: "UoMCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UoMConversions_FromUoMId",
                table: "UoMConversions",
                column: "FromUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_UoMConversions_Id",
                table: "UoMConversions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UoMConversions_ProductId",
                table: "UoMConversions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UoMConversions_ToUoMId",
                table: "UoMConversions",
                column: "ToUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_UoMs_CategoryId",
                table: "UoMs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UoMs_Code",
                table: "UoMs",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UoMCategories_UoMs_BaseUoMId",
                table: "UoMCategories",
                column: "BaseUoMId",
                principalTable: "UoMs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UoMCategories_UoMs_BaseUoMId",
                table: "UoMCategories");

            migrationBuilder.DropTable(
                name: "UoMConversions");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "UoMs");

            migrationBuilder.DropTable(
                name: "UoMCategories");
        }
    }
}
