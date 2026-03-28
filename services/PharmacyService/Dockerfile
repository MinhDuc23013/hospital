FROM node:20-alpine AS base
WORKDIR /app
EXPOSE 5004

FROM base AS build
COPY shared/hospital-shared-js/ /shared/hospital-shared-js/
WORKDIR /shared/hospital-shared-js
RUN npm install && npm run build

WORKDIR /app
COPY services/PharmacyService/package.json ./
RUN npm install
COPY services/PharmacyService/ .
RUN npm run build

FROM base AS final
WORKDIR /app
COPY --from=build /shared/hospital-shared-js /shared/hospital-shared-js
COPY --from=build /app/dist ./dist
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./

ENV PORT=5004
CMD ["node", "dist/server.js"]
