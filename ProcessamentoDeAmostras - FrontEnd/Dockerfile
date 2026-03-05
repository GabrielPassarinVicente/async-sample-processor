FROM node:22-alpine AS build-stage
WORKDIR /app

ARG NUXT_PUBLIC_API_BASE_URL=http://localhost:5000
ENV NUXT_PUBLIC_API_BASE_URL=$NUXT_PUBLIC_API_BASE_URL

COPY package.json yarn.lock ./
RUN yarn install --frozen-lockfile
COPY . .
RUN yarn generate

FROM nginx:stable-alpine AS production-stage
COPY --from=build-stage /app/.output/public /usr/share/nginx/html

RUN chown -R nginx:nginx /usr/share/nginx/html \
    && chmod -R 755 /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
