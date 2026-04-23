FROM node:20-alpine AS build
WORKDIR /app

ARG VITE_API_BASE_URL
ARG VITE_POLLING_MS

COPY package.json package-lock.json ./
# `npm ci` exige que o package-lock.json esteja 100% em sync com o package.json.
# Como esse projeto está com lockfile divergente, usamos `npm install` para conseguir buildar a imagem.
RUN npm install --no-audit --no-fund

COPY . .
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_POLLING_MS=$VITE_POLLING_MS
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
