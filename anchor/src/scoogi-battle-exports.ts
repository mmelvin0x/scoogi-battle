// Here we export some useful types and functions for interacting with the Anchor program.
import { Program } from '@coral-xyz/anchor';
import type { ScoogiBattle } from '../target/types/scoogi_battle';
import { IDL as ScoogiBattleIDL } from '../target/types/scoogi_battle';

// Re-export the generated IDL and type
export { ScoogiBattle, ScoogiBattleIDL };
export type ScoogiBattleProgram = Program<ScoogiBattle>;
