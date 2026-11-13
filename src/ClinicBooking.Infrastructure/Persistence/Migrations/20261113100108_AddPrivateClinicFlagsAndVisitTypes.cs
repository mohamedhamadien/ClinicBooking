using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateClinicFlagsAndVisitTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsultationFee",
                table: "Clinics",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 350m);

            migrationBuilder.AddColumn<bool>(
                name: "EnableGeneralClinicFeatures",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableProceduresDuringShifts",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FollowUpFee",
                table: "Clinics",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 100m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivateClinic",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSingleDoctorClinic",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProcedureFee",
                table: "Clinics",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1200m);

            migrationBuilder.AddColumn<decimal>(
                name: "AgreedFee",
                table: "Appointments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcedureName",
                table: "Appointments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitType",
                table: "Appointments",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Consultation");

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreferredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    PreferredEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false, defaultValue: "Waiting"),
                    OfferedSlotStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfferedSlotEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfferExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookedAppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_ClinicId",
                table: "WaitlistEntries",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_DoctorId",
                table: "WaitlistEntries",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_PatientId",
                table: "WaitlistEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_Tenant_Clinic_Doctor_Date_Status",
                table: "WaitlistEntries",
                columns: new[] { "TenantId", "ClinicId", "DoctorId", "PreferredDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "ConsultationFee",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "EnableGeneralClinicFeatures",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "EnableProceduresDuringShifts",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "FollowUpFee",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "IsPrivateClinic",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "IsSingleDoctorClinic",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "ProcedureFee",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "AgreedFee",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ProcedureName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VisitType",
                table: "Appointments");
        }
    }
}
