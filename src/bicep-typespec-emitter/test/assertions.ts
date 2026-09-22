import { exec } from "child_process";
import path from "path";
import { promisify } from "util";
import { Diagnostic } from "@typespec/compiler";
import { TestEmitterCompileResult } from "@typespec/compiler/testing";
import { expect } from "vitest";

const execAsync = promisify(exec);

export async function expectOutputsToMatchSnapshots(result: TestEmitterCompileResult, snapshotSubDir: string, expectedOutputs: string[]) {
  expect(Object.keys(result.outputs)).toEqual(expectedOutputs);
  for (const expectedOutput of expectedOutputs) {
    await expect(result.outputs[expectedOutput]).toMatchFileSnapshot(`./snapshots/${snapshotSubDir}/${expectedOutput}`);
  }

  await publishBicepExtension(`./test/snapshots/${snapshotSubDir}/index.json`, `./test/bicep/${snapshotSubDir}/test.tgz`);
  await lintBicep(`./test/bicep/${snapshotSubDir}/main.bicep`);
}

function getBicepExePath(): string {
  const bicepPathFromEnv = process.env.BICEP_PATH;
  return bicepPathFromEnv ? bicepPathFromEnv : "bicep";
}

async function publishBicepExtension(pathToIndexDotJson: string, pathToOutput: string): Promise<{ stdout: string; stderr: string }> {
  return await runCommand(`${getBicepExePath()} publish-extension "${path.resolve(pathToIndexDotJson)}" --target "${path.resolve(pathToOutput)}"`);
}

async function lintBicep(pathToBicep: string): Promise<{ stdout: string; stderr: string }> {
  return await runCommand(`${getBicepExePath()} lint "${path.resolve(pathToBicep)}"`);
}

async function runCommand(command: string): Promise<{ stdout: string; stderr: string }> {
  try {
    const result = await execAsync(command, { windowsHide: true });
    return result;
  } catch (error) {
    // this will throw on non-zero exit codes and other errors
    throw new Error(`Failed to execute command '${command}' with error: ${error}`);
  }
}

export interface DiagnosticExclusionMatch {
  code: string;
}

export function removeDiagnostics(diagnostics: readonly Diagnostic[], match: DiagnosticExclusionMatch): readonly Diagnostic[] {
  return diagnostics.filter((diagnostic) => diagnostic.code !== match.code);
}
