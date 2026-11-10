import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home';
import { LoginComponent } from './features/auth/login/login';
import { RegisterComponent } from './features/auth/register/register';
import { DoctorListComponent } from './features/doctors/doctor-list/doctor-list';
import { BookAppointmentComponent } from './features/booking/book-appointment/book-appointment';
import { MyAppointmentsComponent } from './features/patient/my-appointments/my-appointments';
import { DashboardComponent } from './features/admin/dashboard/dashboard';
import { adminGuard, authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', redirectTo: 'auth/login' },
  { path: 'doctors', component: DoctorListComponent },
  { path: 'booking/:id', component: BookAppointmentComponent },
  {
    path: 'my-appointments',
    component: MyAppointmentsComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/dashboard',
    component: DashboardComponent,
    canActivate: [adminGuard]
  },
  {
    path: 'queue/tv',
    loadComponent: () => import('./features/queue/queue-tv-display/queue-tv-display').then((m) => m.QueueTvDisplayComponent)
  },
  { path: '**', redirectTo: '' }
];
