import { Program } from "@typespec/compiler";
import { generateRoot } from "./emitter.js";

export function $onValidate(program: Program) {
  // the emit logic will report diagnostics
  generateRoot(program);
}
