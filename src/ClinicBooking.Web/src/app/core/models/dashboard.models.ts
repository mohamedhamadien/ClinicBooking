export interface DashboardStatsDto {
  totalAppointments: number;
  scheduledAppointments: number;
  confirmedAppointments: number;
  completedAppointments: number;
  cancelledAppointments: number;
  totalDoctors: number;
  totalClinics: number;
  uniquePatients?: number;
}
