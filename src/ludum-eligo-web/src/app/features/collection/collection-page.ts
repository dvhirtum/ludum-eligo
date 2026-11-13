import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Placeholder for the signed-in landing route; the collection page itself is built in task 6.5. */
@Component({
  selector: 'app-collection-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h1>Your collection</h1>`,
})
export class CollectionPage {}
