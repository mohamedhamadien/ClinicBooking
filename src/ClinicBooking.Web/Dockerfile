# ==============================================================================
# Multi-stage Dockerfile for Angular Frontend (Nginx Alpine)
# ==============================================================================

# Stage 1: Build Angular SPA
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npm run build -- --configuration production

# Stage 2: Serve via Nginx
FROM nginx:alpine AS final

# Copy custom Nginx configuration with SPA routing fallback
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled static assets from build stage
COPY --from=build /app/dist/ClinicBooking.Web/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
