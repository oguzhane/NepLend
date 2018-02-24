const Neon = require('@cityofzion/neon-js');
const shortid = require('shortid');

let { sc, rpc, api, CONST, wallet, tx } = Neon;

let Account = wallet.Account;
let util = Neon.u;

Neon.logging.logger.setAll('info');
const privNet = 'http://127.0.0.1:30333';// privNet
const cozNet = '188.68.34.29:10330';// coz Network
const NET = CONST.DEFAULT_RPC.TEST;// mainNet test rpc //https://seed1.neo.org:20331

const neoScanMainNetTest = 'https://neoscan-testnet.io/api/test_net';

const neonDbNet = 'http://127.0.0.1:5000';//update to coz network // this variable can be used as neoscan network
const scHash = 'b60f9f215b285d5ca7ba67d952fd2ff9c7f1b320';// smart contract script hash
let wifAccount1 = '';// borrower
let wifAccount2 = ''; // lender

if(!wifAccount1 && !wifAccount2){
    throw new Error('Both borrower and lender wifKey values cannot be empty!');
}

let account1 = new wallet.Account(wifAccount1);// borrower
let account2 = new wallet.Account(wifAccount2);// lender
let scAddr = wallet.getAddressFromScriptHash(scHash);
var client = new rpc.RPCClient(NET, CONST.RPC_VERSION);

function InvokeContractFactory(operation) {
    return function (callback, net, account, scriptHash, gasCost, intents, ...args) {
        if (typeof account === 'string') {
            account = new Account(account);
        }
        // neonDB replaced as neoscan
        api.neoscan.getBalance(neoScanMainNetTest, account.address)
            .then((balances) => {
                console.log(balances);
                const invoke = { operation, args, scriptHash }
                const unsignedTx = tx.Transaction.createInvocationTx(balances, intents, invoke, gasCost, { version: 1 });
                const signedTx = tx.signTransaction(unsignedTx, account.privateKey);
                rpc.Query.sendRawTransaction(signedTx).execute(net)
                    .then((res) => callback(res))
                    .catch((err) => callback(err));
            })
            .catch(err => callback(err))
    }
}

const mintTokens = (callback, account, amount) => {
    const intents = [
        { assetId: CONST.ASSET_ID.NEO, value: amount, scriptHash: scHash }
    ];
    InvokeContractFactory('mintTokens')(callback, NET, account, scHash, 1, intents);
};

const balanceOf = (callback, accountScriptHash) => {
    client.getStorage(scHash, util.reverseHex(accountScriptHash))
        .then((val) => { callback(val == null ? 0 : util.fixed82num(val)) })
        .catch((err) => callback(err));
};

const totalSupply = (callback) => {
    client.getStorage(scHash, util.str2hexstring("totalSupply"))
        .then((val) => callback(val == null ? 0 : util.fixed82num(val)))
        .catch((err) => console.log(err));
}

const createLoanRequest = (callback, borrower, borrowedAsset, amountBorrowed, amountCollateral, amountPremium, daysToLend) => {
    var loanId = shortid.generate();
    console.log("loanId:", loanId);
    var loanIdRaw = loanId;
    loanId = util.str2hexstring(loanId);
    console.log("loanIdHex:", loanId);
    InvokeContractFactory('createLoanRequest')((cb)=>{
        callback(cb, loanIdRaw);
        if(!cb.result){
            console.log("LoanRequest Creation IS FAILED!! Retry to create.")
        }
    }, NET, borrower, scHash, 1, [], loanId,
        util.reverseHex(borrower.scriptHash), util.reverseHex(borrowedAsset), amountBorrowed, amountCollateral, amountPremium, daysToLend);
}

const lockCollateralForLoan = (callback, loanId, borrower) => {
    loanId = util.str2hexstring(loanId);
    InvokeContractFactory('lockCollateralForLoan')(callback, NET, borrower, scHash, 1, [], loanId, util.reverseHex(borrower.scriptHash));
};

const fundLoan = (callback, loanId, lender, fundAmount, borrowerScriptHash) => {
    loanId = util.str2hexstring(loanId);
    const intents = [
        { assetId: CONST.ASSET_ID.NEO, value: fundAmount, scriptHash: borrowerScriptHash }
    ];
    InvokeContractFactory('fundLoan')(callback, NET, lender, scHash, 1, intents, loanId, util.reverseHex(lender.scriptHash));
}

const cancelLoan = (callback, loanId, canceller) => {
    loanId = util.str2hexstring(loanId);
    InvokeContractFactory('cancelLoan')(callback, NET, canceller, scHash, 1, [], loanId, util.reverseHex(canceller.scriptHash));
}

const claimCollateral = (callback, loanId, lender) => {
    loanId = util.str2hexstring(loanId);
    InvokeContractFactory('claimCollateral')(callback, NET, lender, scHash, 1, [], loanId, util.reverseHex(lender.scriptHash));
}

const repayLoan = (callback, repayAmount, loanId, borrower, lenderScriptHash) => {
    loanId = util.str2hexstring(loanId);
    const intents = [
        { assetId: CONST.ASSET_ID.NEO, value: repayAmount, scriptHash: lenderScriptHash }
    ];
    InvokeContractFactory('repayLoan')(callback, NET, borrower, scHash, 1, intents, loanId, util.reverseHex(borrower.scriptHash));
}

const printLoanStatus=()=>{
    client.getStorage(scHash, util.str2hexstring(loanId)).then((val)=>console.log(val));
}

const factor = 100000000;

var loanId = ''; // <--used by other functions

// mintTokens((res)=>console.log(res), account1, 4/*neo amount for minting*/);

// createLoanRequest((res, loan)=>{console.log(res,'->', loan); loanId=loan}, account1, CONST.ASSET_ID.NEO, 4, 220*factor, 1/*premium*/, 1800/*daysToLend Unix Time Seconds*/);
// lockCollateralForLoan((res)=>console.log(res), loanId, account1);

// fundLoan((res)=>console.log(res), loanId, account2, 4, account1.scriptHash);
// repayLoan((res)=>console.log(res), 5, loanId, account1, account2.scriptHash);
// claimCollateral((res)=>console.log(res), loanId, account2);

// printLoanStatus();// dont forget to set value to loanId variable

// balanceOf((val) => console.log(val), account1.scriptHash);
// totalSupply((val) => console.log(val));
// cancelLoan((res)=>console.log(res), loanId, account1);

// client.getStorage(scHash, util.str2hexstring(loanId)).then((val)=>console.log(val));
// mintTokens((res)=>console.log(res), account1, 5);

/*

Human readable time 	Seconds
1 hour	                3600 seconds
1 day	                86400 seconds
1 week	                604800 seconds
1 month (30.44 days) 	2629743 seconds
1 year (365.24 days) 	31556926 seconds

*/