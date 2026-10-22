// Run after `dotnet restore DotQR.slnx`. Uses Node 18+ built-ins, no npm dependencies.
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));
const solution = await fs.readFile(path.join(root, 'DotQR.slnx'), 'utf8');
const projects = [...solution.matchAll(/<Project Path="([^"]+)"/g)].map(match => match[1]);
const packages = new Set();
for (const project of projects) {
    const assets = JSON.parse(await fs.readFile(path.join(root, path.dirname(project), 'obj/project.assets.json'), 'utf8'));
    for (const [id, info] of Object.entries(assets.libraries)) {
        if (info.type === 'package') packages.add(id);
    }
}

async function json(url) {
    const response = await fetch(url, { signal: AbortSignal.timeout(30000) });
    if (!response.ok) throw new Error(`${url}: HTTP ${response.status}`);
    return response.json();
}

const pending = [...packages];
const results = [];
async function worker() {
    while (pending.length) {
        const id = pending.shift();
        const [name, version] = id.toLowerCase().split('/');
        const leaf = await json(`https://api.nuget.org/v3/registration5-gz-semver2/${name}/${version}.json`);
        const catalog = await json(leaf.catalogEntry);
        results.push({ package: id, deprecated: catalog.deprecation ?? null, listed: leaf.listed, source: leaf.catalogEntry });
    }
}

await Promise.all(Array.from({ length: 6 }, worker));
results.sort((a, b) => a.package.localeCompare(b.package));
const deprecated = results.filter(result => result.deprecated);
await fs.writeFile(path.join(root, 'docs/nuget-deprecation-audit.json'), JSON.stringify({ checkedAt: new Date().toISOString(), packages: results }, null, 2) + '\n');
console.log(JSON.stringify({ checked: results.length, deprecated }, null, 2));
if (deprecated.length) process.exitCode = 1;
