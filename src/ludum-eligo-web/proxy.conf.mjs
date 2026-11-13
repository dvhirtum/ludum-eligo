// Proxies /api to the API. Aspire injects the API URL through service discovery variables.
const target =
  process.env['services__api__https__0'] ?? process.env['services__api__http__0'] ?? 'https://localhost:7001';

export default {
  '/api': {
    target,
    secure: false,
    changeOrigin: false,
    xfwd: true,
  },
};
