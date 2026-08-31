import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class HttpErrorService {

  getErrorMessage(error: HttpErrorResponse): string {

    if (error.error?.message) {
      return error.error.message;
    }

    if (error.status === 0) {
      return 'Unable to connect to the backend server.';
    }

    if (error.status === 400) {
      return 'Invalid request.';
    }

    if (error.status === 404) {
      return 'Requested resource was not found.';
    }

    if (error.status >= 500) {
      return 'Server error. Please try again later.';
    }

    return 'Something went wrong. Please try again.';
  }
}