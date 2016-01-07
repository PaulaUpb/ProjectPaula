using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InternalCourseID",
                table: "Course",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InternalCourseID", table: "Course");
        }
    }
}
