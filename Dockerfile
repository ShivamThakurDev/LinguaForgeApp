# Frontend (Angular SSR / Node). Build context is the repo root.
# Multi-stage: build the SSR bundle on the full Node image, run it on a slim one.
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=4000

# The Angular application builder bundles the SSR server (express included), so only
# the dist output is needed at runtime — no node_modules.
COPY --from=build /app/dist/language-learning-app ./dist/language-learning-app

EXPOSE 4000
CMD ["node", "dist/language-learning-app/server/server.mjs"]
