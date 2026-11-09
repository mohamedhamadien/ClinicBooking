import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TranslationService } from '../services/translation.service';
import { environment } from '../../../environments/environment';

export const apiInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const translationService = inject(TranslationService);

  const token = authService.getAccessToken();
  const lang = translationService.isArabic() ? 'ar-EG' : 'en-US';
  const tenantSlug = environment.defaultTenantSlug;

  // Clone headers
  let headers = req.headers
    .set('X-Tenant-Slug', tenantSlug)
    .set('Accept-Language', lang);

  if (token && !req.headers.has('Authorization')) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  const authReq = req.clone({ headers });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Auto-refresh token on 401 Unauthorized (unless request is already login/refresh)
      if (
        error.status === 401 &&
        !req.url.includes('/auth/login') &&
        !req.url.includes('/auth/refresh-token')
      ) {
        return authService.refreshToken().pipe(
          switchMap((authResponse) => {
            const newToken = authResponse.accessToken || (authResponse as any).AccessToken;
            const retriedReq = req.clone({
              headers: req.headers
                .set('Authorization', `Bearer ${newToken}`)
                .set('X-Tenant-Slug', tenantSlug)
                .set('Accept-Language', lang)
            });
            return next(retriedReq);
          }),
          catchError((refreshErr) => {
            authService.logout();
            return throwError(() => refreshErr);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
