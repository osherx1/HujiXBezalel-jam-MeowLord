mergeInto(LibraryManager.library, {
  SaveScoreToFirebase: function(nicknamePtr, score, finishTime, callbackSuccessPtr, callbackErrorPtr) {
    var nickname = UTF8ToString(nicknamePtr);
    var callbackSuccess = UTF8ToString(callbackSuccessPtr);
    var callbackError = UTF8ToString(callbackErrorPtr);
    console.log('[FirebaseBridge.jslib] SaveScoreToFirebase called with nickname:', nickname, 'score:', score, 'finishTime:', finishTime);
    if (typeof SaveScoreToFirebaseJS === "function") {
      SaveScoreToFirebaseJS(nickname, score, finishTime, callbackSuccess, callbackError);
    } else {
      console.error("SaveScoreToFirebaseJS is not defined!");
      SendMessage('FirebaseBridge', 'OnSubmitScoreError', 'SaveScoreToFirebaseJS is not defined!');
    }
  },
  GetLeaderboardFromFirebase: function(callbackSuccessPtr, callbackErrorPtr) {
    var callbackSuccess = UTF8ToString(callbackSuccessPtr);
    var callbackError = UTF8ToString(callbackErrorPtr);
    console.log('[FirebaseBridge.jslib] GetLeaderboardFromFirebase called');
    if (typeof GetLeaderboardFromFirebaseJS === "function") {
      GetLeaderboardFromFirebaseJS(callbackSuccess,callbackError);
    } else {
      console.error("GetLeaderboardFromFirebaseJS is not defined!");
      SendMessage('FirebaseBridge', 'OnFetchLeaderboardError', 'GetLeaderboardFromFirebaseJS is not defined!');
    }
  },
  InitializeFirebase: function() {
    console.log('[FirebaseBridge.jslib] InitializeFirebase called');
    if (typeof InitializeFirebase === "function") {
      InitializeFirebase();
    } else {
      console.error("InitializeFirebase is not defined!");
      SendMessage('FirebaseBridge', 'OnFirebaseInitialized', 'error:InitializeFirebase is not defined!');
    }
  }
});