import { ComponentFixture } from '@angular/core/testing';

/** Lets promise continuations (e.g. after a flushed HTTP request) run, then waits for change detection. */
export async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  await fixture.whenStable();
}
