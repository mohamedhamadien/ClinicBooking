namespace ClinicBooking.Domain.Constants;

public static class ClinicPermissions
{
    // Screen Permissions (Which screens/tabs the staff member is allowed to see)
    public const string ScreenOverview = "Screen.Overview";
    public const string ScreenQueue = "Screen.Queue";
    public const string ScreenConsultation = "Screen.Consultation";
    public const string ScreenMedicalRecords = "Screen.MedicalRecords";
    public const string ScreenBilling = "Screen.Billing";
    public const string ScreenAppointments = "Screen.Appointments";
    public const string ScreenDoctors = "Screen.Doctors";
    public const string ScreenClinics = "Screen.Clinics";
    public const string ScreenSpecialties = "Screen.Specialties";
    public const string ScreenStaff = "Screen.Staff";
    public const string ScreenInventory = "Screen.Inventory";
    public const string ScreenAudit = "Screen.Audit";
    public const string ScreenNotifications = "Screen.Notifications";

    // Functional Action Permissions (Which specific actions the staff member can execute)
    public const string QueueCheckIn = "Queue.CheckIn";
    public const string QueueCallPatient = "Queue.CallPatient";
    public const string ConsultationCreate = "Consultation.Create";
    public const string PrescriptionsCreate = "Prescriptions.Create";
    public const string MedicalRecordsUpdate = "MedicalRecords.Update";
    public const string BillingCreateInvoice = "Billing.CreateInvoice";
    public const string BillingCollectPayment = "Billing.CollectPayment";
    public const string AppointmentsManage = "Appointments.Manage";
    public const string DoctorsManage = "Doctors.Manage";
    public const string StaffManage = "Staff.Manage";
    public const string InventoryManage = "Inventory.Manage";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        // Screens
        new(ScreenOverview, "Overview Screen", "شاشة المتابعة العامة", "Screens", "View dashboard summary and operational statistics"),
        new(ScreenQueue, "Waiting Queue Screen", "شاشة طابور الانتظار والاستقبال", "Screens", "View daily reception patient queue and attendance"),
        new(ScreenConsultation, "Consultation & SOAP Screen", "غرفة الكشف الطبي السريري", "Screens", "Access doctor examination room and SOAP notes"),
        new(ScreenMedicalRecords, "Patient Medical Records Screen", "السجلات الطبية للمرضى", "Screens", "Access comprehensive patient clinical records"),
        new(ScreenBilling, "Billing & Invoices Screen", "شاشة الفواتير والتحصيل", "Screens", "Access billing, invoicing and financial management"),
        new(ScreenAppointments, "Appointments Screen", "إدارة المواعيد والحجوزات", "Screens", "Access full appointments schedule"),
        new(ScreenDoctors, "Doctors Screen", "إدارة الأطباء", "Screens", "Access doctors list, clinics and schedules"),
        new(ScreenClinics, "Clinics Screen", "إدارة العيادات والمراكز", "Screens", "Access clinics and branch configuration"),
        new(ScreenSpecialties, "Specialties Screen", "إدارة التخصصات الطبية", "Screens", "Access medical specialties list"),
        new(ScreenStaff, "Staff & Permissions Screen", "إدارة الموظفين والصلاحيات", "Screens", "Manage clinic staff accounts and custom permissions"),
        new(ScreenInventory, "Inventory Screen", "شاشة المخزون والمستلزمات", "Screens", "Manage clinic medical supplies and pharmacy inventory"),
        new(ScreenAudit, "Audit Trail & Compliance Screen", "شاشة سجل الرقابة والتدقيق الأمني", "Screens", "View full enterprise tamper-evident audit logs and data history"),
        new(ScreenNotifications, "Notifications & Reminders Screen", "شاشة التنبيهات ورسائل التذكير", "Screens", "View reminder message logs and dispatch WhatsApp/SMS notifications"),

        // Functions
        new(QueueCheckIn, "Check-in Patients", "تسجيل حضور المرضى", "Functions", "Mark patient as arrived and assign sequential queue token"),
        new(QueueCallPatient, "Call Patient to Room", "النداء على المريض للكشف", "Functions", "Call waiting patient into the examination room"),
        new(ConsultationCreate, "Create Consultation & Diagnosis", "إجراء الكشف الطبي والتشخيص", "Functions", "Record clinical SOAP notes, vital signs and diagnoses"),
        new(PrescriptionsCreate, "Create & Print Prescriptions", "تحرير وطباعة الروشتة الطبية", "Functions", "Generate official Rx prescriptions for patients"),
        new(MedicalRecordsUpdate, "Update Medical History & Allergies", "تحديث التاريخ المرضي والحساسيات", "Functions", "Modify patient blood group, allergies, and chronic conditions"),
        new(BillingCreateInvoice, "Generate Invoices", "إنشاء وإصدار الفواتير", "Functions", "Create itemized invoices for consultations and procedures"),
        new(BillingCollectPayment, "Collect Payments", "تحصيل وتسجيل المدفوعات", "Functions", "Process cash, card, and insurance payments for invoices"),
        new(AppointmentsManage, "Manage Appointments", "حجز وتعديل وإلغاء المواعيد", "Functions", "Schedule, reschedule, or cancel patient bookings"),
        new(DoctorsManage, "Manage Doctors & Schedules", "إدارة الأطباء ومواعيد العمل", "Functions", "Add/edit doctor profiles, specialties and availability"),
        new(StaffManage, "Manage Staff & Permissions", "إدارة الموظفين وتعيين الصلاحيات", "Functions", "Create staff accounts and grant/revoke screen and action permissions"),
        new(InventoryManage, "Manage Inventory & Stock", "إدارة المخزون والتوريد والصرف", "Functions", "Add supplies, adjust stock levels and manage inventory items")
    ];

    public static readonly Dictionary<string, IReadOnlyList<string>> RolePresets = new()
    {
        ["Receptionist"] =
        [
            ScreenOverview,
            ScreenQueue,
            ScreenAppointments,
            ScreenNotifications,
            ScreenBilling,
            QueueCheckIn,
            AppointmentsManage,
            BillingCollectPayment
        ],
        ["Nurse"] =
        [
            ScreenOverview,
            ScreenQueue,
            ScreenConsultation,
            ScreenMedicalRecords,
            QueueCheckIn,
            QueueCallPatient,
            MedicalRecordsUpdate
        ],
        ["Accountant"] =
        [
            ScreenOverview,
            ScreenBilling,
            BillingCreateInvoice,
            BillingCollectPayment
        ],
        ["Doctor"] =
        [
            ScreenOverview,
            ScreenQueue,
            ScreenConsultation,
            ScreenMedicalRecords,
            QueueCallPatient,
            ConsultationCreate,
            PrescriptionsCreate,
            MedicalRecordsUpdate
        ],
        ["Staff"] =
        [
            ScreenOverview,
            ScreenQueue,
            ScreenAppointments,
            QueueCheckIn
        ],
        ["ClinicAdmin"] = All.Select(p => p.Key).ToList(),
        ["Admin"] = All.Select(p => p.Key).ToList()
    };
}

public sealed record PermissionDefinition(
    string Key,
    string NameEn,
    string NameAr,
    string Category,
    string Description);
