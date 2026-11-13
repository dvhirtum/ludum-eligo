// Starts `ng serve` with the port and HTTPS certificate that Aspire provides.
import { spawn } from 'node:child_process';

const args = ['serve', '--port', process.env.PORT ?? '4200'];
if (process.env.SSL_CERT_PATH && process.env.SSL_KEY_PATH) {
  args.push('--ssl', '--ssl-cert', process.env.SSL_CERT_PATH, '--ssl-key', process.env.SSL_KEY_PATH);
}

const child = spawn('npx', ['ng', ...args], { stdio: 'inherit', shell: true });
child.on('exit', (code) => process.exit(code ?? 0));
