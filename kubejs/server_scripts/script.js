// priority: 0

console.info('Loading server script.');

ServerEvents.tags('item', event => {
	// https://minecraft.fandom.com/wiki/Redstone_components
	event.add('crafters:redstone_components', ...[
		'minecraft:redstone',
		'minecraft:redstone_torch',
		'minecraft:lever',
		'minecraft:repeater',
		'minecraft:comparator',
		'minecraft:piston',
		'minecraft:sticky_piston',
	]);
})