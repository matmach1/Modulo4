using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riesgos.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Reportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Ubicacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CategoriaSugerida = table.Column<string>(type: "TEXT", nullable: true),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", nullable: false),
                    Criticidad = table.Column<string>(type: "TEXT", nullable: false),
                    VersionClasificacion = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    CreadorId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<long>(type: "INTEGER", nullable: false),
                    FechaInicioProgreso = table.Column<long>(type: "INTEGER", nullable: true),
                    UsuarioInicioProgresoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AccionRealizada = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ObservacionCierre = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FechaCierre = table.Column<long>(type: "INTEGER", nullable: true),
                    UsuarioCierreId = table.Column<int>(type: "INTEGER", nullable: true),
                    RowVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reportes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reportes_Usuarios_CreadorId",
                        column: x => x.CreadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reportes_Usuarios_UsuarioCierreId",
                        column: x => x.UsuarioCierreId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reportes_Usuarios_UsuarioInicioProgresoId",
                        column: x => x.UsuarioInicioProgresoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_CreadorId_FechaCreacion",
                table: "Reportes",
                columns: new[] { "CreadorId", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_Estado_Criticidad",
                table: "Reportes",
                columns: new[] { "Estado", "Criticidad" });

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_FechaCreacion_Id",
                table: "Reportes",
                columns: new[] { "FechaCreacion", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_UsuarioCierreId",
                table: "Reportes",
                column: "UsuarioCierreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_UsuarioInicioProgresoId",
                table: "Reportes",
                column: "UsuarioInicioProgresoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reportes");
        }
    }
}
