using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {   

            migrationBuilder.CreateTable(
                name: "CategoryFilter",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    CategoryFilterID = table.Column<int>(nullable: true),
                    CourseCatalogInternalID = table.Column<string>(nullable: true),
                    IsTopLevel = table.Column<bool>(nullable: false),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryFilter", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CategoryFilter_CategoryFilter_CategoryFilterID",
                        column: x => x.CategoryFilterID,
                        principalTable: "CategoryFilter",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryFilter_CourseCatalog_CourseCatalogInternalID",
                        column: x => x.CourseCatalogInternalID,
                        principalTable: "CourseCatalog",
                        principalColumn: "InternalID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryCourse",
                columns: table => new
                {
                    CourseId = table.Column<string>(nullable: false),
                    CategoryFilterId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryCourse", x => new { x.CourseId, x.CategoryFilterId });
                    table.ForeignKey(
                        name: "FK_CategoryCourse_CategoryFilter_CategoryFilterId",
                        column: x => x.CategoryFilterId,
                        principalTable: "CategoryFilter",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryCourse_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {            
            migrationBuilder.DropTable(
                name: "CategoryCourse");

            migrationBuilder.DropTable(
                name: "CategoryFilter");            
        }
    }
}
