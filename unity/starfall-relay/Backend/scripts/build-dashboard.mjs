import {readFile,mkdir,writeFile} from 'node:fs/promises';
const url=process.env.STARFALL_SUPABASE_URL,key=process.env.STARFALL_PUBLISHABLE_KEY;
if(!url||!key||!url.startsWith('https://'))throw Error('Set STARFALL_SUPABASE_URL and STARFALL_PUBLISHABLE_KEY. Use only a public key.');
if(key.startsWith('sb_secret_'))throw Error('A server secret cannot be used in the dashboard.');
if(key.split('.').length===3){const claims=JSON.parse(Buffer.from(key.split('.')[1],'base64url').toString());if(claims.role!=='anon')throw Error('Only the anon/public client key is allowed.');}
const js=value=>JSON.stringify(value).replaceAll('<','\\u003c');
const html=(await readFile(new URL('../dashboard/index.template.html',import.meta.url),'utf8')).replaceAll('__API_URL__',js(url)).replaceAll('__PUBLIC_KEY__',js(key));
await mkdir(new URL('../dashboard/dist/',import.meta.url),{recursive:true});
await writeFile(new URL('../dashboard/dist/index.html',import.meta.url),html);
console.log('Dashboard built. Host dashboard/dist with HTTPS and add its origin and callback URL to Supabase.');
