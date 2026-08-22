# ==========================================
# STAGE 1: Build React/Vite Application
# ==========================================
FROM node:22-alpine AS build
WORKDIR /app

# Accept build argument for API URL
ARG VITE_API_BASE_URL=https://localhost:7070/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Copy package files and install dependencies
COPY package.json package-lock.json* ./
RUN npm ci

# Copy source code and build static bundle
COPY . .
RUN npm run build

# ==========================================
# STAGE 2: Serve with Nginx Alpine (Ultra-lightweight ~25MB)
# ==========================================
FROM nginx:alpine AS final

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built static files from build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
