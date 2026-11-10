import { Injectable, inject } from '@angular/core';
import { TranslationService } from './translation.service';
import { AppointmentDto } from '../models/appointment.models';
import { DoctorDto, ClinicDto } from '../models/doctor.models';
import {
  InvoiceDto,
  PatientMedicalRecordDto,
  PrescriptionDto,
  QueueItemDto
} from '../models/clinical.models';
import { DashboardStatsDto } from '../models/dashboard.models';
import { StaffMemberDto } from '../models/staff.models';

@Injectable({
  providedIn: 'root'
})
export class ReportingService {
  private readonly translation = inject(TranslationService);

  /**
   * Triggers a browser download of a CSV file with UTF-8 BOM for Arabic/English compatibility.
   */
  public downloadCsv(filename: string, headers: string[], rows: (string | number | undefined | null)[][]): void {
    const escapeCell = (val: string | number | undefined | null): string => {
      if (val === null || val === undefined) return '""';
      const str = String(val).replace(/"/g, '""');
      return `"${str}"`;
    };

    const headerLine = headers.map(escapeCell).join(',');
    const dataLines = rows.map((r) => r.map(escapeCell).join(','));
    const csvContent = [headerLine, ...dataLines].join('\r\n');

    // Prepend UTF-8 BOM so Excel correctly detects Arabic encoding
    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', filename.endsWith('.csv') ? filename : `${filename}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  /**
   * Opens a printable popup window formatted with healthcare letterhead, styling, and triggers browser print.
   */
  public printHtmlReport(title: string, contentHtml: string): void {
    const isAr = this.translation.isArabic();
    const dir = isAr ? 'rtl' : 'ltr';
    const lang = isAr ? 'ar' : 'en';

    const printWindow = window.open('', '_blank', 'width=950,height=800');
    if (!printWindow) {
      alert(isAr ? 'يرجى السماح بالنوافذ المنبثقة لطباعة التقرير.' : 'Please allow popup windows to print the report.');
      return;
    }

    const html = `<!DOCTYPE html>
<html lang="${lang}" dir="${dir}">
<head>
  <meta charset="utf-8">
  <title>${title}</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Cairo:wght@400;600;700;800&family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
  <style>
    * { box-sizing: border-box; }
    body {
      font-family: 'Cairo', 'Inter', -apple-system, sans-serif;
      color: #1e293b;
      background: #ffffff;
      margin: 0;
      padding: 24px;
      line-height: 1.5;
      font-size: 13px;
    }
    .report-letterhead {
      display: flex;
      justify-content: space-between;
      align-items: center;
      border-bottom: 2px solid #2563eb;
      padding-bottom: 14px;
      margin-bottom: 20px;
    }
    .brand-section {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .brand-logo {
      font-size: 36px;
      line-height: 1;
    }
    .brand-title {
      font-size: 20px;
      font-weight: 800;
      color: #1e3a8a;
      margin: 0;
    }
    .brand-sub {
      font-size: 12px;
      color: #64748b;
      margin: 2px 0 0;
    }
    .report-meta-box {
      text-align: ${isAr ? 'left' : 'right'};
      font-size: 11px;
      color: #64748b;
    }
    .report-title-strip {
      background: #f1f5f9;
      padding: 10px 16px;
      border-radius: 6px;
      margin-bottom: 20px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .report-title {
      font-size: 16px;
      font-weight: 700;
      color: #0f172a;
      margin: 0;
    }
    .report-date {
      font-size: 12px;
      color: #475569;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 14px;
      margin-bottom: 20px;
      font-size: 12px;
    }
    th, td {
      border: 1px solid #e2e8f0;
      padding: 8px 12px;
      text-align: ${isAr ? 'right' : 'left'};
    }
    th {
      background-color: #f8fafc;
      color: #334155;
      font-weight: 700;
      text-transform: uppercase;
      font-size: 11px;
      letter-spacing: 0.03em;
    }
    tr:nth-child(even) td {
      background-color: #fafafa;
    }
    .badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;
    }
    .badge-primary { background: #dbeafe; color: #1e40af; }
    .badge-success { background: #dcfce7; color: #15803d; }
    .badge-warning { background: #fef3c7; color: #b45309; }
    .badge-danger { background: #fee2e2; color: #b91c1c; }
    .info-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 12px;
      margin-bottom: 16px;
    }
    .info-card {
      background: #f8fafc;
      padding: 10px 14px;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
    }
    .info-label { font-size: 11px; color: #64748b; font-weight: 600; }
    .info-value { font-size: 13px; font-weight: 700; color: #0f172a; margin-top: 2px; }
    .report-signatures {
      display: flex;
      justify-content: space-between;
      margin-top: 40px;
      padding-top: 20px;
      border-top: 1px dashed #cbd5e1;
    }
    .sign-box {
      text-align: center;
      width: 200px;
    }
    .sign-line {
      border-bottom: 1px solid #94a3b8;
      height: 40px;
      margin-bottom: 6px;
    }
    .report-footer {
      margin-top: 30px;
      padding-top: 10px;
      border-top: 1px solid #e2e8f0;
      font-size: 10px;
      color: #94a3b8;
      display: flex;
      justify-content: space-between;
    }
    @media print {
      body { padding: 0; }
      .no-print { display: none !important; }
      @page { margin: 1.5cm; }
    }
  </style>
</head>
<body>
  <div class="report-letterhead">
    <div class="brand-section">
      <span class="brand-logo">🏥</span>
      <div>
        <h1 class="brand-title">${isAr ? 'عيادات أبيكس الطبية المتقدمة' : 'Apex Health Clinical OS'}</h1>
        <p class="brand-sub">${isAr ? 'نظام السجلات والتشغيل الطبي الموحد' : 'Centralized Clinical & Health Information System'}</p>
      </div>
    </div>
    <div class="report-meta-box">
      <div><strong>${isAr ? 'رقم التقرير:' : 'Doc Ref:'}</strong> REP-${Date.now().toString().slice(-6)}</div>
      <div><strong>${isAr ? 'تاريخ التوليد:' : 'Generated:'}</strong> ${new Date().toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</div>
      <div><strong>${isAr ? 'الحالة:' : 'Status:'}</strong> ${isAr ? 'معتمد رسمياً' : 'Official Record'}</div>
    </div>
  </div>

  <div class="report-title-strip">
    <h2 class="report-title">${title}</h2>
    <span class="report-date">${new Date().toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit' })}</span>
  </div>

  ${contentHtml}

  <div class="report-signatures">
    <div class="sign-box">
      <div class="sign-line"></div>
      <small>${isAr ? 'توقيع الموظف المسؤول' : 'Authorized Personnel'}</small>
    </div>
    <div class="sign-box">
      <div class="sign-line"></div>
      <small>${isAr ? 'ختم واعتماد المنشأة الطبية' : 'Clinic Stamp / Certification'}</small>
    </div>
  </div>

  <div class="report-footer">
    <span>${isAr ? 'تم استخراج هذا التقرير إلكترونياً من نظام إدارة العيادات Apex Clinic OS' : 'Electronically generated by Apex Clinic OS Healthcare Management System'}</span>
    <span>Page 1 / 1</span>
  </div>

  <script>
    window.onload = function() {
      setTimeout(function() {
        window.print();
      }, 350);
    };
  </script>
</body>
</html>`;

    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
  }

  // =========================================================================
  // SPECIFIC PRINTABLE REPORT GENERATORS
  // =========================================================================

  /**
   * 1. Official Prescription Printout (Rx)
   */
  public printPrescription(rx: PrescriptionDto, clinicName?: string): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? `روشتة علاجية إلكترونية رقم ${rx.prescriptionNumber}` : `Official Electronic Prescription - #${rx.prescriptionNumber}`;

    const itemsRows = rx.items
      .map(
        (it, idx) => `
        <tr>
          <td>#${idx + 1}</td>
          <td><strong>${it.medicationName}</strong></td>
          <td>${it.dosage}</td>
          <td>${it.frequency}</td>
          <td>${it.duration}</td>
          <td>${it.instructions || '-'}</td>
        </tr>`
      )
      .join('');

    const html = `
      <div class="info-grid">
        <div class="info-card">
          <div class="info-label">${isAr ? 'المريض' : 'Patient Name'}</div>
          <div class="info-value">👤 ${rx.patientName || (isAr ? 'مريض' : 'Patient')}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'الفرع الطبي' : 'Clinic Branch'}</div>
          <div class="info-value">🏥 ${clinicName || rx.clinicName || (isAr ? 'الفرع الرئيسي' : 'Central Healthcare')}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'التشخيص الطبي' : 'Clinical Diagnosis'}</div>
          <div class="info-value">🩺 ${rx.diagnosis}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'تاريخ الكشف والإصدار' : 'Prescription Date'}</div>
          <div class="info-value">📅 ${new Date(rx.issuedAtUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</div>
        </div>
      </div>

      <h3 style="margin-top: 24px; color: #1e40af; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;">
        💊 ${isAr ? 'الأدوية والجرعات العلاجية الموصوفة (Rx)' : 'Prescribed Medications (Rx)'}
      </h3>
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>${isAr ? 'الدواء والتركيز' : 'Medication Name'}</th>
            <th>${isAr ? 'الجرعة' : 'Dosage'}</th>
            <th>${isAr ? 'التكرار' : 'Frequency'}</th>
            <th>${isAr ? 'المدة' : 'Duration'}</th>
            <th>${isAr ? 'تعليمات الاستخدام' : 'Instructions'}</th>
          </tr>
        </thead>
        <tbody>
          ${itemsRows}
        </tbody>
      </table>

      ${
        rx.generalInstructions
          ? `<div style="background: #eff6ff; padding: 12px 16px; border-radius: 6px; border-right: 4px solid #2563eb; margin-top: 14px;">
               <strong>${isAr ? 'تعليمات وإرشادات الطبيب للمريض:' : 'Doctor Special Instructions:'}</strong>
               <p style="margin: 4px 0 0;">${rx.generalInstructions}</p>
             </div>`
          : ''
      }
    `;

    this.printHtmlReport(title, html);
  }

  /**
   * 2. Official Tax Invoice & Cash Receipt Printout
   */
  public printInvoiceReceipt(inv: InvoiceDto): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? `فاتورة ضريبية رسمية وإيصال تحصيل #${inv.invoiceNumber}` : `Official Tax Invoice & Receipt - #${inv.invoiceNumber}`;

    const itemsRows = inv.items
      .map(
        (it) => `
        <tr>
          <td>${it.description}</td>
          <td style="text-align: center;">${it.quantity}</td>
          <td>${it.unitPrice} ${isAr ? 'ج.م' : 'EGP'}</td>
          <td><strong>${it.totalPrice} ${isAr ? 'ج.م' : 'EGP'}</strong></td>
        </tr>`
      )
      .join('');

    const html = `
      <div class="info-grid">
        <div class="info-card">
          <div class="info-label">${isAr ? 'المريض' : 'Patient'}</div>
          <div class="info-value">👤 ${inv.patientName || inv.patientId.slice(0, 8)}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'الفرع الطبي' : 'Clinic Branch'}</div>
          <div class="info-value">🏥 ${inv.clinicName || (isAr ? 'فرع العيادة' : 'Clinic Branch')}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'طريقة السداد' : 'Payment Method'}</div>
          <div class="info-value">💳 ${inv.paymentMethod}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'حالة الفاتورة' : 'Invoice Status'}</div>
          <div class="info-value">
            <span class="badge ${inv.status === 'Paid' ? 'badge-success' : 'badge-warning'}">
              ${inv.status === 'Paid' ? (isAr ? 'مدفوعة بالكامل' : 'Paid in Full') : (isAr ? 'قيد السداد' : 'Pending')}
            </span>
          </div>
        </div>
      </div>

      <h3 style="margin-top: 24px; color: #1e40af; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;">
        🧾 ${isAr ? 'بنود الخدمات الطبية المقدمة' : 'Itemized Medical Charges'}
      </h3>
      <table>
        <thead>
          <tr>
            <th>${isAr ? 'الخدمة / الإجراء' : 'Service Description'}</th>
            <th style="text-align: center;">${isAr ? 'العدد' : 'Qty'}</th>
            <th>${isAr ? 'السعر الفردي' : 'Unit Price'}</th>
            <th>${isAr ? 'الإجمالي' : 'Total'}</th>
          </tr>
        </thead>
        <tbody>
          ${itemsRows}
        </tbody>
        <tfoot>
          <tr>
            <th colspan="3" style="text-align: ${isAr ? 'left' : 'right'}; font-size: 13px;">${isAr ? 'المبلغ الإجمالي المستحق:' : 'Total Payable Amount:'}</th>
            <th style="font-size: 14px; color: #1e40af;">${inv.totalAmount} ${isAr ? 'ج.م' : 'EGP'}</th>
          </tr>
        </tfoot>
      </table>

      ${
        inv.status === 'Paid'
          ? `<div style="background: #f0fdf4; border: 1px solid #86efac; border-radius: 6px; padding: 12px; margin-top: 14px; text-align: center; color: #166534; font-weight: 700;">
               ✅ ${isAr ? 'تم سداد هذا المبلغ بالكامل في خزينة العيادة بتاريخ ' : 'Paid in full at clinic reception on '}${new Date(inv.paidAtUtc || inv.issuedAtUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}
             </div>`
          : ''
      }
    `;

    this.printHtmlReport(title, html);
  }

  /**
   * 3. Patient Complete Medical Dossier (EMR) Printout
   */
  public printPatientEmr(rec: PatientMedicalRecordDto): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? `الملف الطبي الشامل للمريض: ${rec.patientName}` : `Comprehensive Patient EMR Dossier: ${rec.patientName}`;

    const consultRows = rec.consultations
      .map(
        (c) => `
        <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; margin-bottom: 10px;">
          <div style="display: flex; justify-content: space-between; font-weight: 700;">
            <span style="color: #1e40af;">🩺 ${c.diagnosis}</span>
            <small style="color: #64748b;">${new Date(c.consultationDateUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</small>
          </div>
          <p style="margin: 6px 0 0; font-size: 12px;"><strong>${isAr ? 'شكوى المريض:' : 'Chief Complaint:'}</strong> ${c.chiefComplaint}</p>
          ${c.treatmentPlan ? `<p style="margin: 4px 0 0; font-size: 12px; color: #475569;"><strong>${isAr ? 'الخطة العلاجية:' : 'Treatment Plan:'}</strong> ${c.treatmentPlan}</p>` : ''}
        </div>`
      )
      .join('');

    const rxRows = rec.prescriptions
      .map(
        (p) => `
        <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; margin-bottom: 10px;">
          <div style="display: flex; justify-content: space-between; font-weight: 700;">
            <span>💊 #${p.prescriptionNumber}</span>
            <small style="color: #64748b;">${new Date(p.issuedAtUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</small>
          </div>
          <small style="color: #475569;">${isAr ? 'التشخيص: ' : 'Diagnosis: '}${p.diagnosis}</small>
          <ul style="margin: 6px 0 0; padding-${isAr ? 'right' : 'left'}: 20px; font-size: 12px;">
            ${p.items.map((i) => `<li><strong>${i.medicationName}</strong> (${i.dosage} - ${i.frequency} - ${i.duration})</li>`).join('')}
          </ul>
        </div>`
      )
      .join('');

    const html = `
      <div class="info-grid">
        <div class="info-card">
          <div class="info-label">${isAr ? 'اسم المريض' : 'Patient Name'}</div>
          <div class="info-value">👤 ${rec.patientName}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'تاريخ الميلاد' : 'Date of Birth'}</div>
          <div class="info-value">🎂 ${rec.dateOfBirth}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'النوع' : 'Gender'}</div>
          <div class="info-value">${rec.gender}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'فصيلة الدم' : 'Blood Group'}</div>
          <div class="info-value" style="color: #b91c1c;">🩸 ${rec.bloodGroup || (isAr ? 'غير محدد' : 'Unspecified')}</div>
        </div>
      </div>

      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 14px;">
        <div style="background: #fffbeb; border: 1px solid #fde68a; border-radius: 6px; padding: 12px;">
          <strong style="color: #b45309;">⚠️ ${isAr ? 'حساسية الأدوية والأغذية المعروفة:' : 'Known Drug & Food Allergies:'}</strong>
          <p style="margin: 4px 0 0; font-size: 12px;">${rec.allergies || (isAr ? 'لا توجد حساسية مسجلة.' : 'No known allergies reported.')}</p>
        </div>
        <div style="background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 12px;">
          <strong style="color: #1e40af;">🩺 ${isAr ? 'الأمراض الطبية المزمنة:' : 'Chronic Medical Conditions:'}</strong>
          <p style="margin: 4px 0 0; font-size: 12px;">${rec.chronicConditions || (isAr ? 'لا توجد أمراض مزمنة مسجلة.' : 'None reported.')}</p>
        </div>
      </div>

      <h3 style="margin-top: 24px; color: #1e40af; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;">
        📋 ${isAr ? 'سجل الكشوفات والتشخيصات السابقة' : 'Past Clinical Consultations & Diagnoses'} (${rec.consultations.length})
      </h3>
      ${consultRows || `<p style="color: #64748b;">${isAr ? 'لا توجد كشوف سابقة مسجلة.' : 'No past consultations recorded.'}</p>`}

      <h3 style="margin-top: 24px; color: #1e40af; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;">
        💊 ${isAr ? 'الروشتات الطبية السابقة' : 'Historical Electronic Prescriptions'} (${rec.prescriptions.length})
      </h3>
      ${rxRows || `<p style="color: #64748b;">${isAr ? 'لا توجد روشتات سابقة مسجلة.' : 'No past prescriptions on file.'}</p>`}
    `;

    this.printHtmlReport(title, html);
  }

  /**
   * 4. Executive Clinic Performance & Operational Summary Report
   */
  public printExecutiveSummary(
    stats: DashboardStatsDto | null,
    totalDoctors: number,
    clinicsCount: number,
    appointmentsCount: number
  ): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? 'تقرير الأداء التشغيلي والإحصائي العام للعيادات' : 'Clinic Operational Performance & Analytics Report';

    const html = `
      <div class="info-grid">
        <div class="info-card">
          <div class="info-label">${isAr ? 'إجمالي الحجوزات المسجلة' : 'Total Bookings'}</div>
          <div class="info-value" style="font-size: 20px; color: #2563eb;">${stats?.totalAppointments || appointmentsCount}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'المواعيد المؤكدة والمكتملة' : 'Completed Encounters'}</div>
          <div class="info-value" style="font-size: 20px; color: #16a34a;">${stats?.completedAppointments || 0}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'المواعيد المجدولة القادمة' : 'Scheduled / Pending'}</div>
          <div class="info-value" style="font-size: 20px; color: #ea580c;">${stats?.scheduledAppointments || 0}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'إجمالي الكادر الطبي النشط' : 'Practicing Medical Staff'}</div>
          <div class="info-value" style="font-size: 20px; color: #0284c7;">${stats?.totalDoctors || totalDoctors} ${isAr ? 'طبيب' : 'Doctors'}</div>
        </div>
      </div>

      <h3 style="margin-top: 24px; color: #1e40af; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;">
        📊 ${isAr ? 'مؤشرات الأداء الرئيسية (KPIs)' : 'Key Operational Performance Indicators'}
      </h3>
      <table>
        <thead>
          <tr>
            <th>${isAr ? 'المؤشر التشغيلي' : 'Operational Metric'}</th>
            <th>${isAr ? 'القيمة الإحصائية' : 'Value'}</th>
            <th>${isAr ? 'الحالة والتقييم' : 'Performance Status'}</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>${isAr ? 'نسبة إتمام المواعيد الطبية (Completion Rate)' : 'Encounter Completion Rate'}</td>
            <td><strong>${stats?.totalAppointments ? Math.round((stats.completedAppointments / stats.totalAppointments) * 100) : 0}%</strong></td>
            <td><span class="badge badge-success">${isAr ? 'ممتاز' : 'Optimal'}</span></td>
          </tr>
          <tr>
            <td>${isAr ? 'عدد الفروع والمرافق المعتمدة' : 'Operational Facilities'}</td>
            <td><strong>${clinicsCount} ${isAr ? 'فروع' : 'Branches'}</strong></td>
            <td><span class="badge badge-primary">${isAr ? 'نشط' : 'Active'}</span></td>
          </tr>
          <tr>
            <td>${isAr ? 'متوسط حمل الحجوزات لكل طبيب' : 'Average Patient Load per Physician'}</td>
            <td><strong>${totalDoctors > 0 ? (appointmentsCount / totalDoctors).toFixed(1) : 0}</strong></td>
            <td><span class="badge badge-primary">${isAr ? 'ضمن المعدل القياسي' : 'Normal Range'}</span></td>
          </tr>
        </tbody>
      </table>
    `;

    this.printHtmlReport(title, html);
  }

  /**
   * 5. Waiting Room Queue & Dispatch Report
   */
  public printQueueReport(queueItems: QueueItemDto[], clinicName?: string): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? 'كشف طابور الاستقبال وغرف الكشف اليومي' : 'Daily Reception Queue & Token Sheet';

    const rows = queueItems
      .map(
        (q) => `
        <tr>
          <td><strong style="color: #2563eb; font-size: 14px;">#${q.queueNumber ?? '-'}</strong></td>
          <td><strong>${q.patientName}</strong></td>
          <td>${q.doctorName}</td>
          <td>${q.clinicName}</td>
          <td>${new Date(q.startTimeUtc).toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit' })}</td>
          <td>
            <span class="badge ${q.status === 'InConsultation' ? 'badge-primary' : q.status === 'CheckedIn' ? 'badge-success' : 'badge-warning'}">
              ${q.status === 'CheckedIn' ? (isAr ? 'في الانتظار' : 'In Waiting Room') : q.status === 'InConsultation' ? (isAr ? 'داخل الفحص' : 'In Consultation') : q.status}
            </span>
          </td>
        </tr>`
      )
      .join('');

    const html = `
      <div class="info-grid">
        <div class="info-card">
          <div class="info-label">${isAr ? 'الفرع الطبي' : 'Clinic Branch'}</div>
          <div class="info-value">🏥 ${clinicName || (isAr ? 'كافة الفروع' : 'All Facilities')}</div>
        </div>
        <div class="info-card">
          <div class="info-label">${isAr ? 'إجمالي الحالات اليوم' : 'Total Queue Tokens'}</div>
          <div class="info-value">🛎️ ${queueItems.length} ${isAr ? 'مرضى' : 'Patients'}</div>
        </div>
      </div>

      <table>
        <thead>
          <tr>
            <th>${isAr ? 'رقم الدور' : 'Token #'}</th>
            <th>${isAr ? 'المريض' : 'Patient'}</th>
            <th>${isAr ? 'الطبيب المعالج' : 'Doctor'}</th>
            <th>${isAr ? 'فرع العيادة' : 'Branch'}</th>
            <th>${isAr ? 'الموعد' : 'Time'}</th>
            <th>${isAr ? 'الحالة الحالية' : 'Queue Status'}</th>
          </tr>
        </thead>
        <tbody>
          ${rows || `<tr><td colspan="6" style="text-align: center;">${isAr ? 'لا يوجد مرضى في الطابور اليوم.' : 'No patients in queue.'}</td></tr>`}
        </tbody>
      </table>
    `;

    this.printHtmlReport(title, html);
  }

  /**
   * 6. Appointments Schedule Roster Report
   */
  public printAppointmentsSchedule(appointments: AppointmentDto[]): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? 'جدول المواعيد والحجوزات الطبية' : 'Appointments Master Schedule Report';

    const rows = appointments
      .map(
        (a) => `
        <tr>
          <td><strong>${a.patientName || a.patientId.slice(0, 8)}</strong></td>
          <td>${a.doctorName || a.doctorId.slice(0, 8)}</td>
          <td>${a.clinicName || '-'}</td>
          <td>${new Date(a.startTimeUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')} ${new Date(a.startTimeUtc).toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit' })}</td>
          <td>
            <span class="badge ${a.status === 'Completed' ? 'badge-success' : a.status.startsWith('Cancelled') ? 'badge-danger' : 'badge-primary'}">
              ${a.status}
            </span>
          </td>
        </tr>`
      )
      .join('');

    const html = `
      <table>
        <thead>
          <tr>
            <th>${isAr ? 'المريض' : 'Patient'}</th>
            <th>${isAr ? 'الطبيب' : 'Doctor'}</th>
            <th>${isAr ? 'الفرع' : 'Clinic'}</th>
            <th>${isAr ? 'التاريخ والوقت' : 'Date & Time'}</th>
            <th>${isAr ? 'الحالة' : 'Status'}</th>
          </tr>
        </thead>
        <tbody>
          ${rows || `<tr><td colspan="5" style="text-align: center;">${isAr ? 'لا توجد مواعيد مسجلة.' : 'No appointments.'}</td></tr>`}
        </tbody>
      </table>
    `;

    this.printHtmlReport(title, html);
  }

  /**
   * 7. Patient Booking Slip Card Printout (for patient's My Appointments)
   */
  public printPatientAppointmentSlip(appt: AppointmentDto): void {
    const isAr = this.translation.isArabic();
    const title = isAr ? `بطاقة تأكيد حجز الموعد #${appt.id.slice(0, 8)}` : `Appointment Confirmation Slip - #${appt.id.slice(0, 8)}`;

    const html = `
      <div style="border: 2px solid #2563eb; border-radius: 8px; padding: 20px; background: #fafafa;">
        <div style="text-align: center; margin-bottom: 20px;">
          <h2 style="color: #1e40af; margin: 0;">🏥 ${isAr ? 'تأكيد الحجز الطبي المعتمد' : 'Confirmed Medical Booking'}</h2>
          <p style="color: #64748b; margin: 4px 0 0;">${isAr ? 'يرجى إبراز هذه البطاقة لموظف الاستقبال عند الحضور' : 'Please present this slip at the clinic reception on arrival'}</p>
        </div>

        <div class="info-grid">
          <div class="info-card">
            <div class="info-label">${isAr ? 'الطبيب المعالج' : 'Consulting Doctor'}</div>
            <div class="info-value">👨‍⚕️ ${appt.doctorName || 'Doctor'}</div>
          </div>
          <div class="info-card">
            <div class="info-label">${isAr ? 'فرع العيادة' : 'Clinic Branch'}</div>
            <div class="info-value">📍 ${appt.clinicName || 'Clinic Branch'}</div>
          </div>
          <div class="info-card">
            <div class="info-label">${isAr ? 'تاريخ ووقت الموعد' : 'Date & Time'}</div>
            <div class="info-value">📅 ${new Date(appt.startTimeUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')} (${new Date(appt.startTimeUtc).toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit' })})</div>
          </div>
          <div class="info-card">
            <div class="info-label">${isAr ? 'حالة الموعد' : 'Booking Status'}</div>
            <div class="info-value">
              <span class="badge badge-success">✅ ${appt.status}</span>
            </div>
          </div>
        </div>

        <div style="background: #eff6ff; border: 1px dashed #3b82f6; border-radius: 6px; padding: 12px; margin-top: 16px; text-align: center;">
          <small style="color: #1e40af; font-weight: 600;">
            ${isAr ? '💡 يُرجى التكرم بالحضور قبل الموعد بـ 15 دقيقة لإتمام تسجيل الحضور واستلام رقم الدور' : '💡 Please arrive 15 minutes prior to your slot for reception check-in and token issuance'}
          </small>
        </div>
      </div>
    `;

    this.printHtmlReport(title, html);
  }

  // =========================================================================
  // CSV DATA EXPORT METHODS
  // =========================================================================

  public exportAppointmentsCsv(appointments: AppointmentDto[]): void {
    const isAr = this.translation.isArabic();
    const headers = isAr
      ? ['معرف الموعد', 'المريض', 'الطبيب', 'الفرع', 'تاريخ البدء', 'تاريخ الانتهاء', 'الحالة']
      : ['Appointment ID', 'Patient', 'Doctor', 'Clinic', 'Start Time', 'End Time', 'Status'];

    const rows = appointments.map((a) => [
      a.id,
      a.patientName || a.patientId,
      a.doctorName || a.doctorId,
      a.clinicName || '',
      new Date(a.startTimeUtc).toLocaleString(),
      new Date(a.endTimeUtc).toLocaleString(),
      a.status
    ]);

    this.downloadCsv(`clinic_appointments_${new Date().toISOString().slice(0, 10)}`, headers, rows);
  }

  public exportInvoicesCsv(invoices: InvoiceDto[]): void {
    const isAr = this.translation.isArabic();
    const headers = isAr
      ? ['رقم الفاتورة', 'المريض', 'الفرع', 'تاريخ الإصدار', 'المبلغ الإجمالي', 'طريقة السداد', 'الحالة', 'تاريخ السداد']
      : ['Invoice #', 'Patient', 'Clinic', 'Issued Date', 'Total Amount', 'Payment Method', 'Status', 'Paid Date'];

    const rows = invoices.map((inv) => [
      inv.invoiceNumber,
      inv.patientName || inv.patientId,
      inv.clinicName || '',
      new Date(inv.issuedAtUtc).toLocaleDateString(),
      inv.totalAmount,
      inv.paymentMethod,
      inv.status,
      inv.paidAtUtc ? new Date(inv.paidAtUtc).toLocaleDateString() : ''
    ]);

    this.downloadCsv(`clinic_invoices_ledger_${new Date().toISOString().slice(0, 10)}`, headers, rows);
  }

  public exportQueueCsv(items: QueueItemDto[]): void {
    const isAr = this.translation.isArabic();
    const headers = isAr
      ? ['رقم الدور', 'المريض', 'الطبيب', 'الفرع', 'وقت الموعد', 'حالة الدور']
      : ['Token #', 'Patient', 'Doctor', 'Clinic', 'Scheduled Time', 'Queue Status'];

    const rows = items.map((q) => [
      q.queueNumber ?? '',
      q.patientName,
      q.doctorName,
      q.clinicName,
      new Date(q.startTimeUtc).toLocaleTimeString(),
      q.status
    ]);

    this.downloadCsv(`clinic_waiting_queue_${new Date().toISOString().slice(0, 10)}`, headers, rows);
  }

  public exportDoctorsCsv(doctors: DoctorDto[]): void {
    const isAr = this.translation.isArabic();
    const headers = isAr
      ? ['اسم الطبيب', 'رقم الترخيص', 'التخصص', 'مدة الكشف (دقيقة)', 'سعر الكشف', 'عدد الفروع']
      : ['Doctor Name', 'License Number', 'Specialty', 'Slot Duration (Mins)', 'Consultation Fee', 'Branches Count'];

    const rows = doctors.map((d) => [
      d.fullName,
      d.licenseNumber,
      d.specialtyName || '',
      d.defaultSlotDurationMinutes,
      d.doctorClinics.length > 0 && d.doctorClinics[0].consultationFee ? d.doctorClinics[0].consultationFee : 'Standard',
      d.doctorClinics.length
    ]);

    this.downloadCsv(`clinic_doctors_directory_${new Date().toISOString().slice(0, 10)}`, headers, rows);
  }

  public exportStaffCsv(staffMembers: StaffMemberDto[]): void {
    const isAr = this.translation.isArabic();
    const headers = isAr
      ? ['اسم الموظف', 'البريد الإلكتروني', 'الدور الوظيفي', 'الفرع المعين', 'عدد الصلاحيات', 'الحالة']
      : ['Staff Name', 'Email Address', 'Role', 'Assigned Clinic', 'Permissions Count', 'Status'];

    const rows = staffMembers.map((s) => [
      s.fullName,
      s.email,
      s.role,
      s.clinicName || 'All Branches / Global',
      s.permissions.length,
      s.isActive ? 'Active' : 'Suspended'
    ]);

    this.downloadCsv(`clinic_staff_audit_${new Date().toISOString().slice(0, 10)}`, headers, rows);
  }
}
