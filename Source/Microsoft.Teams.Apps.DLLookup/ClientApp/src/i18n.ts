﻿// <copyright file="i18n.ts" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import Backend from 'i18next-http-backend';

i18n
    .use(Backend)
    .use(initReactI18next) // passes i18n down to react-i18next
    .init({
        lng: window.navigator.language,
        fallbackLng: 'en-US',
        keySeparator: false, // we do not use keys in form messages.welcome 
        interpolation: {
            escapeValue: false // react already safes from xss
        },
        load: 'currentOnly'
    });


export default i18n;