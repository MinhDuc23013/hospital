"use client";

// Minimal toast hook — wraps Radix Toast state for use in Client Components.
// Inspired by shadcn/ui toast implementation.
import * as React from "react";

const TOAST_LIMIT = 3;
const TOAST_REMOVE_DELAY = 3000;

type ToastVariant = "default" | "destructive";

export interface ToastMessage {
  id: string;
  title?: string;
  description?: string;
  variant?: ToastVariant;
  open: boolean;
}

type ToastAction =
  | { type: "ADD"; toast: Omit<ToastMessage, "id" | "open"> }
  | { type: "DISMISS"; id: string }
  | { type: "REMOVE"; id: string };

let toastCount = 0;

function toastReducer(state: ToastMessage[], action: ToastAction): ToastMessage[] {
  switch (action.type) {
    case "ADD":
      return [
        { ...action.toast, id: String(++toastCount), open: true },
        ...state,
      ].slice(0, TOAST_LIMIT);
    case "DISMISS":
      return state.map((t) => (t.id === action.id ? { ...t, open: false } : t));
    case "REMOVE":
      return state.filter((t) => t.id !== action.id);
    default:
      return state;
  }
}

// Module-level listeners for cross-component access
const listeners: Array<(state: ToastMessage[]) => void> = [];
let memoryState: ToastMessage[] = [];

function dispatch(action: ToastAction) {
  memoryState = toastReducer(memoryState, action);
  listeners.forEach((listener) => listener(memoryState));
}

export function toast(message: Omit<ToastMessage, "id" | "open">) {
  dispatch({ type: "ADD", toast: message });

  setTimeout(() => {
    // Auto-dismiss after delay
    const id = String(toastCount);
    dispatch({ type: "DISMISS", id });
    setTimeout(() => dispatch({ type: "REMOVE", id }), 300);
  }, TOAST_REMOVE_DELAY);
}

export function useToast() {
  const [toasts, setToasts] = React.useState<ToastMessage[]>(memoryState);

  React.useEffect(() => {
    listeners.push(setToasts);
    return () => {
      const index = listeners.indexOf(setToasts);
      if (index > -1) listeners.splice(index, 1);
    };
  }, []);

  return {
    toasts,
    toast,
    dismiss: (id: string) => dispatch({ type: "DISMISS", id }),
  };
}
