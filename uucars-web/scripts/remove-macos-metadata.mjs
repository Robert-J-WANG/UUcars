import { readdir, rm } from "node:fs/promises";
import path from "node:path";

const publicDirectory = path.resolve(import.meta.dirname, "../public");

async function removeMacOsMetadata(directory) {
  const entries = await readdir(directory, { withFileTypes: true });

  for (const entry of entries) {
    const entryPath = path.join(directory, entry.name);

    if (entry.name === ".DS_Store") {
      await rm(entryPath, { force: true });
      continue;
    }

    if (entry.isDirectory()) {
      await removeMacOsMetadata(entryPath);
    }
  }
}

await removeMacOsMetadata(publicDirectory);
