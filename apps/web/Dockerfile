# syntax=docker/dockerfile:1

FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
# Client ID Google là định danh công khai (không phải secret). Giá trị thật
# của prod do render.yaml truyền qua buildArgs (chỗ source of truth); dòng này
# là fallback cho docker build cục bộ không build-arg.
ARG VITE_GOOGLE_CLIENT_ID=795347854507-b91gdgji9etht4qtptgt5oj8phbbm33r.apps.googleusercontent.com
ENV VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID
RUN pnpm build

FROM caddy:2
# Image caddy:2 setcap cap_net_bind_service=+ep cho /usr/bin/caddy; Render chạy
# container với no-new-privileges → exec binary có file capability bị EPERM
# ("operation not permitted"). Bỏ capability — ta listen $PORT / :80 với root.
RUN setcap -r /usr/bin/caddy
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist /srv
EXPOSE 80 443
