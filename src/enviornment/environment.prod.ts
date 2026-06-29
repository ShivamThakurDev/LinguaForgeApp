// Production API base URL. Baked at build time via the angular.json `production`
// fileReplacements. Override per environment by editing this value (or templating it
// in CI before `ng build`). For the local docker-compose stack this is the API gateway
// URL reachable from the browser.
export const environment = {
  production: true,
  apiBaseUrl: 'http://localhost:8080/api',
};
