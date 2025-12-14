// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getFirestore } from "firebase/firestore";

// TODO: Replace the following with your app's Firebase project configuration
// You can get this from the Firebase Console -> Project Settings -> General -> Your apps
const firebaseConfig = {
    apiKey: "AIzaSyBQfaBy3Ra5pEzB1uuhSl3uCT6q6ydtdQM",
    authDomain: "taranapsu.firebaseapp.com",
    projectId: "taranapsu",
    storageBucket: "taranapsu.appspot.com",
    messagingSenderId: "232170901734",
    appId: "1:232170901734:web:a1e4217a89871b61196b27"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
export const db = getFirestore(app);
