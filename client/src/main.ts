import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';

import {enableProdMode} from '@angular/core';
import { Title } from '@angular/platform-browser';

enableProdMode();


platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.error(err));
