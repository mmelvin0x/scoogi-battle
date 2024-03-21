import * as anchor from '@coral-xyz/anchor';

import {
  ASSOCIATED_TOKEN_PROGRAM_ID,
  Account,
  TOKEN_PROGRAM_ID,
  createMint,
  getAccount,
  getOrCreateAssociatedTokenAccount,
  mintTo,
} from '@solana/spl-token';
import {
  Connection,
  Keypair,
  LAMPORTS_PER_SOL,
  PublicKey,
  SystemProgram,
} from '@solana/web3.js';

import { Program } from '@coral-xyz/anchor';
import { ScoogiBattle } from '../target/types/scoogi_battle';

async function airdrop(
  connection: Connection,
  to: PublicKey,
  lamports = 2 * LAMPORTS_PER_SOL
) {
  await connection.confirmTransaction(
    await connection.requestAirdrop(to, lamports),
    'confirmed'
  );
}

const ADMIN_SEED = Buffer.from('admin');
const BATTLE_SEED = Buffer.from('battle');
const TOKEN_ACCOUNT_SEED = Buffer.from('token_account');

describe('🐸 Scoogi Battle 🤺', () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env());
  const connection = anchor.getProvider().connection;

  const program = anchor.workspace.ScoogiBattle as Program<ScoogiBattle>;

  let mint: PublicKey;
  let playerOneTokenAccount: Account;
  let playerTwoTokenAccount: Account;
  let adminTokenAccount: Account;
  let battleAccountAddress: PublicKey;
  let battleTokenAccountAddress: PublicKey;
  const burnFeeBps = new anchor.BN(100);
  const battlePrice = new anchor.BN(10_000);

  const startBattleId = new anchor.BN(new Date().getTime());
  const admin = anchor.Wallet.local();
  const playerOne = Keypair.generate();
  const playerTwo = Keypair.generate();
  const [adminAccount] = PublicKey.findProgramAddressSync(
    [ADMIN_SEED],
    program.programId
  );

  beforeAll(async () => {
    try {
      await airdrop(connection, admin.publicKey);
      await airdrop(connection, playerOne.publicKey);
      await airdrop(connection, playerTwo.publicKey);
    } catch (error) {
      console.error('Error:', error);
    }

    mint = await createMint(
      connection,
      admin.payer,
      admin.publicKey,
      admin.publicKey,
      9
    );

    playerOneTokenAccount = await getOrCreateAssociatedTokenAccount(
      connection,
      admin.payer,
      mint,
      playerOne.publicKey
    );

    playerTwoTokenAccount = await getOrCreateAssociatedTokenAccount(
      connection,
      admin.payer,
      mint,
      playerTwo.publicKey
    );

    adminTokenAccount = await getOrCreateAssociatedTokenAccount(
      connection,
      admin.payer,
      mint,
      admin.publicKey
    );

    await mintTo(
      connection,
      admin.payer,
      mint,
      playerOneTokenAccount.address,
      admin.publicKey,
      100_000 * LAMPORTS_PER_SOL
    );

    await mintTo(
      connection,
      admin.payer,
      mint,
      playerTwoTokenAccount.address,
      admin.publicKey,
      100_000 * LAMPORTS_PER_SOL
    );

    [battleTokenAccountAddress] = PublicKey.findProgramAddressSync(
      [TOKEN_ACCOUNT_SEED, playerOne.publicKey.toBuffer()],
      program.programId
    );

    [battleAccountAddress] = PublicKey.findProgramAddressSync(
      [BATTLE_SEED, playerOne.publicKey.toBuffer()],
      program.programId
    );
  });

  it('Initialization', async () => {
    const tx = await program.methods
      .initialize(burnFeeBps, battlePrice)
      .accounts({
        admin: admin.publicKey,
        adminAccount,
        mint,
        systemProgram: SystemProgram.programId,
      })
      .signers([admin.payer])
      .rpc();

    await connection.confirmTransaction(tx, 'confirmed');

    const adminAccountData = await program.account.admin.fetch(adminAccount);

    expect(adminAccountData.admin.toBase58()).toBe(admin.publicKey.toBase58());
    expect(adminAccountData.mint.toBase58()).toBe(mint.toBase58());
    expect(adminAccountData.burnFeeBps.toString()).toBe(burnFeeBps.toString());
    expect(adminAccountData.battlePrice.toString()).toBe(
      battlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)).toString()
    );
  });

  describe('Full battle w/ Player One as Winner', () => {
    it('Creates a battle', async () => {
      const beforeTokenBalance = new anchor.BN(
        (await getAccount(connection, playerOneTokenAccount.address)).amount
      );

      const tx = await program.methods
        .createBattle(startBattleId)
        .accounts({
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerOneTokenAccount: playerOneTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerOne])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const afterTokenBalance = new anchor.BN(
        (await getAccount(connection, playerOneTokenAccount.address)).amount
      );
      const battleAccountData = await program.account.battle.fetch(
        battleAccountAddress
      );

      expect(afterTokenBalance.toString()).toBe(
        beforeTokenBalance
          .sub(battlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)))
          .toString()
      );
      expect(battleAccountData.battleId.toString()).toBe(
        startBattleId.toString()
      );
      expect(battleAccountData.playerOne.toBase58()).toBe(
        playerOne.publicKey.toBase58()
      );
      expect(battleAccountData.playerTwo.toBase58()).toBe(
        new PublicKey(0).toBase58()
      );
      expect(battleAccountData.battleStatus).toStrictEqual({ pending: {} });
    });

    it('Joins a battle', async () => {
      const beforeTokenBalance = new anchor.BN(
        (await getAccount(connection, playerTwoTokenAccount.address)).amount
      );

      const tx = await program.methods
        .joinBattle(startBattleId)
        .accounts({
          playerTwo: playerTwo.publicKey,
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerTwoTokenAccount: playerTwoTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerTwo])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const afterTokenBalance = new anchor.BN(
        (await getAccount(connection, playerTwoTokenAccount.address)).amount
      );
      const battleAccountData = await program.account.battle.fetch(
        battleAccountAddress
      );

      expect(afterTokenBalance.toString()).toBe(
        beforeTokenBalance
          .sub(battlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)))
          .toString()
      );
      expect(battleAccountData.battleId.toString()).toBe(
        startBattleId.toString()
      );
      expect(battleAccountData.playerOne.toBase58()).toBe(
        playerOne.publicKey.toBase58()
      );
      expect(battleAccountData.playerTwo.toBase58()).toBe(
        playerTwo.publicKey.toBase58()
      );
      expect(battleAccountData.battleStatus).toStrictEqual({ inProgress: {} });
    });

    it('Records a battle', async () => {
      const battleResult = 0;
      const winner = playerOne;
      const winnerTokenAccount = playerOneTokenAccount;

      const beforeWinnerTokenAccount = await getAccount(
        connection,
        winnerTokenAccount.address
      );
      const beforeBattleTokenAccount = await getAccount(
        connection,
        battleTokenAccountAddress
      );

      const tx = await program.methods
        .recordBattleResult(battleResult, startBattleId)
        .accounts({
          winner: winner.publicKey,
          playerOne: playerOne.publicKey,
          playerTwo: playerTwo.publicKey,
          admin: admin.publicKey,
          adminAccount,
          adminTokenAccount: adminTokenAccount.address,
          battleAccount: battleAccountAddress,
          battleTokenAccount: battleTokenAccountAddress,
          winnerTokenAccount: winnerTokenAccount.address,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([winner])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const afterWinnerTokenAccount = await getAccount(
        connection,
        winnerTokenAccount.address
      );
      const burnAmount = burnFeeBps
        .mul(battlePrice.mul(new anchor.BN(2 * LAMPORTS_PER_SOL)))
        .div(new anchor.BN(10_000));

      expect(afterWinnerTokenAccount.amount.toString()).toBe(
        new anchor.BN(
          beforeWinnerTokenAccount.amount + beforeBattleTokenAccount.amount
        )
          .sub(burnAmount)
          .toString()
      );

      try {
        await program.account.battle.fetch(battleAccountAddress);
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
      }
    });
  });

  describe('Full battle w/ Player Two as Winner', () => {
    it('Creates a battle', async () => {
      const beforeTokenBalance = new anchor.BN(
        (await getAccount(connection, playerOneTokenAccount.address)).amount
      );

      const tx = await program.methods
        .createBattle(startBattleId)
        .accounts({
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerOneTokenAccount: playerOneTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerOne])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const afterTokenBalance = new anchor.BN(
        (await getAccount(connection, playerOneTokenAccount.address)).amount
      );
      const battleAccountData = await program.account.battle.fetch(
        battleAccountAddress
      );

      expect(afterTokenBalance.toString()).toBe(
        beforeTokenBalance
          .sub(battlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)))
          .toString()
      );
      expect(battleAccountData.battleId.toString()).toBe(
        startBattleId.toString()
      );
      expect(battleAccountData.playerOne.toBase58()).toBe(
        playerOne.publicKey.toBase58()
      );
      expect(battleAccountData.playerTwo.toBase58()).toBe(
        new PublicKey(0).toBase58()
      );
      expect(battleAccountData.battleStatus).toStrictEqual({ pending: {} });
    });

    it('Joins a battle', async () => {
      const beforeTokenBalance = new anchor.BN(
        (await getAccount(connection, playerTwoTokenAccount.address)).amount
      );

      const tx = await program.methods
        .joinBattle(startBattleId)
        .accounts({
          playerTwo: playerTwo.publicKey,
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerTwoTokenAccount: playerTwoTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerTwo])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const afterTokenBalance = new anchor.BN(
        (await getAccount(connection, playerTwoTokenAccount.address)).amount
      );
      const battleAccountData = await program.account.battle.fetch(
        battleAccountAddress
      );

      expect(afterTokenBalance.toString()).toBe(
        beforeTokenBalance
          .sub(battlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)))
          .toString()
      );
      expect(battleAccountData.battleId.toString()).toBe(
        startBattleId.toString()
      );
      expect(battleAccountData.playerOne.toBase58()).toBe(
        playerOne.publicKey.toBase58()
      );
      expect(battleAccountData.playerTwo.toBase58()).toBe(
        playerTwo.publicKey.toBase58()
      );
      expect(battleAccountData.battleStatus).toStrictEqual({ inProgress: {} });
    });

    it('Records a battle', async () => {
      const battleResult = 1;
      const winner = playerTwo;
      const winnerTokenAccount = playerTwoTokenAccount;

      const beforeWinnerTokenAccount = await getAccount(
        connection,
        winnerTokenAccount.address
      );
      const beforeBattleTokenAccount = await getAccount(
        connection,
        battleTokenAccountAddress
      );

      const tx = await program.methods
        .recordBattleResult(battleResult, startBattleId)
        .accounts({
          winner: winner.publicKey,
          playerOne: playerOne.publicKey,
          playerTwo: playerTwo.publicKey,
          admin: admin.publicKey,
          adminAccount,
          adminTokenAccount: adminTokenAccount.address,
          battleAccount: battleAccountAddress,
          battleTokenAccount: battleTokenAccountAddress,
          winnerTokenAccount: winnerTokenAccount.address,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([winner])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const afterWinnerTokenAccount = await getAccount(
        connection,
        winnerTokenAccount.address
      );
      const burnAmount = burnFeeBps
        .mul(battlePrice.mul(new anchor.BN(2 * LAMPORTS_PER_SOL)))
        .div(new anchor.BN(10_000));

      expect(afterWinnerTokenAccount.amount.toString()).toBe(
        new anchor.BN(
          beforeWinnerTokenAccount.amount + beforeBattleTokenAccount.amount
        )
          .sub(burnAmount)
          .toString()
      );

      try {
        await program.account.battle.fetch(battleAccountAddress);
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
      }
    });
  });

  describe('Battle with withdraw', () => {
    it('withdraws', async () => {
      startBattleId.add(new anchor.BN(new Date().getTime()));
      let tx = await program.methods
        .createBattle(startBattleId)
        .accounts({
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerOneTokenAccount: playerOneTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerOne])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      tx = await program.methods
        .withdrawFromBattle(startBattleId)
        .accounts({
          playerOne: playerOne.publicKey,
          admin: admin.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          battleTokenAccount: battleTokenAccountAddress,
          playerOneTokenAccount: playerOneTokenAccount.address,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerOne])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      try {
        await program.account.battle.fetch(battleAccountAddress);
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
      }
    });
  });

  describe('Admin', () => {
    it('withdraws', async () => {
      startBattleId.add(new anchor.BN(new Date().getTime()));

      let tx = await program.methods
        .createBattle(startBattleId)
        .accounts({
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerOneTokenAccount: playerOneTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerOne])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      tx = await program.methods
        .joinBattle(startBattleId)
        .accounts({
          playerTwo: playerTwo.publicKey,
          playerOne: playerOne.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          playerTwoTokenAccount: playerTwoTokenAccount.address,
          battleTokenAccount: battleTokenAccountAddress,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([playerTwo])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      tx = await program.methods
        .adminWithdrawal(startBattleId)
        .accounts({
          admin: admin.publicKey,
          playerOne: playerOne.publicKey,
          playerTwo: playerTwo.publicKey,
          adminAccount,
          battleAccount: battleAccountAddress,
          battleTokenAccount: battleTokenAccountAddress,
          playerOneTokenAccount: playerOneTokenAccount.address,
          playerTwoTokenAccount: playerTwoTokenAccount.address,
          mint,
          systemProgram: SystemProgram.programId,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .signers([admin.payer])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      try {
        await program.account.battle.fetch(battleAccountAddress);
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
      }
    });

    it('updates battle price', async () => {
      const newBattlePrice = new anchor.BN(20_000);

      const tx = await program.methods
        .updateBattlePrice(newBattlePrice)
        .accounts({
          admin: admin.publicKey,
          adminAccount,
          mint,
        })
        .signers([admin.payer])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const adminAccountData = await program.account.admin.fetch(adminAccount);

      expect(adminAccountData.battlePrice.toString()).toBe(
        newBattlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)).toString()
      );
    });

    it('updates burn fee bps', async () => {
      const newBurnFeeBps = new anchor.BN(200);

      const tx = await program.methods
        .updateBurnFeeBps(newBurnFeeBps)
        .accounts({
          admin: admin.publicKey,
          adminAccount,
          systemProgram: SystemProgram.programId,
        })
        .signers([admin.payer])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const adminAccountData = await program.account.admin.fetch(adminAccount);

      expect(adminAccountData.burnFeeBps.toString()).toBe(
        newBurnFeeBps.toString()
      );
    });

    it('updates the mint', async () => {
      const newMint = await createMint(
        connection,
        admin.payer,
        admin.publicKey,
        admin.publicKey,
        9
      );

      const tx = await program.methods
        .updateMint()
        .accounts({
          admin: admin.publicKey,
          adminAccount,
          mint: newMint,
        })
        .signers([admin.payer])
        .rpc();

      await connection.confirmTransaction(tx, 'confirmed');

      const adminAccountData = await program.account.admin.fetch(adminAccount);

      expect(adminAccountData.mint.toBase58()).toBe(newMint.toBase58());
    });
  });
});
